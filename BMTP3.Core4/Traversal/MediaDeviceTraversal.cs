using BMTP3.Core4.Models;
using BMTP3.Core4.Utilities;
using MediaDevices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Traversal;


/// <summary>
/// Traverses content on an MTP device.
/// </summary>
/// <remarks>
/// This type does not own the lifetime of the underlying <see cref="MtpDeviceSession"/>.
/// Yielded <see cref="SourceTraversalItem"/> instances may contain <see cref="MediaDeviceContent"/>
/// objects that continue to access the device after traversal has finished.
/// The caller must therefore keep the session alive until all yielded content and streams
/// have been fully consumed.
/// </remarks>
[SupportedOSPlatform("windows7.0")]

internal sealed class MediaDeviceTraversal : ISourceTraversal
{
	private readonly MtpDeviceSession _session;
	private readonly IMtpGatekeeper _gatekeeper;

	public MediaDeviceTraversal(MtpDeviceSession session, IMtpGatekeeper gatekeeper)
	{
		_session = session ?? throw new ArgumentNullException(nameof(session));
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	public async IAsyncEnumerable<SourceTraversalItem> TraverseAsync(
		SourceTraversalRequest request,
		IProgress<SourceTraversalProgress>? progress,
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(request);

		int dirCount = 0;
		int fileCount = 0;

		await foreach((MediaFileInfo file, string fileName, string relativePath) in EnumerateRecursiveAsync(
						  request.SourcePath,
						  relativePrefix: "",
						  recursive: request.Recursive,
						  onDirectoryEntered: () => dirCount++,
						  cancellationToken).ConfigureAwait(false))
		{
			cancellationToken.ThrowIfCancellationRequested();

			fileCount++;

			progress?.Report(new SourceTraversalProgress
			{
				DirectoriesTraversed = dirCount,
				FilesDiscovered = fileCount,
			});

			if(!GlobMatcher.IsIncluded(relativePath, request.IncludePatterns, request.ExcludePatterns))
				continue;

			SourceTraversalItem item = await _gatekeeper.ExecuteAsync(_ =>
			{
				DateTimeOffset? created = ToUtcOffsetOrNull(file.CreationTime);
				DateTimeOffset? modified = ToUtcOffsetOrNull(file.LastWriteTime);
				DateTimeOffset? authored = ToUtcOffsetOrNull(file.DateAuthored);

				SourceTraversalItem result = new()
				{
					Id = file.FullName,
					SourcePath = file.FullName,
					RelativePath = relativePath,
					FileName = fileName,
					Content = new MediaDeviceContent(file, _gatekeeper),
					DateCreated = created,
					DateModified = modified,
					DateAuthored = authored,
					DateAccessed = null // MediaDevices does not expose last accessed time.
				};

				return Task.FromResult(result);
			}, cancellationToken).ConfigureAwait(false);

			yield return item;
		}
	}

	private async IAsyncEnumerable<(MediaFileInfo File, string FileName, string RelativePath)> EnumerateRecursiveAsync(
		string devicePath,
		string relativePrefix,
		bool recursive,
		Action onDirectoryEntered,
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
	{
		(List<(MediaFileInfo File, string Name)> files, List<(string FullName, string Name)> dirs) = await _gatekeeper.ExecuteAsync(_ =>
		{
			MediaDirectoryInfo dir = _session.Device.GetDirectoryInfo(devicePath);

			List<(MediaFileInfo File, string Name)> files = dir
				.EnumerateFiles()
				.Select(file => (file, file.Name))
				.ToList();

			List<(string FullName, string Name)> dirs = recursive
				? dir.EnumerateDirectories()
					.Select(subDir => (subDir.FullName, subDir.Name))
					.ToList()
				: new List<(string FullName, string Name)>();

			return Task.FromResult((files, dirs));
		}, cancellationToken).ConfigureAwait(false);

		foreach((MediaFileInfo file, string fileName) in files)
		{
			cancellationToken.ThrowIfCancellationRequested();

			string rel = string.IsNullOrEmpty(relativePrefix)
				? fileName
				: $"{relativePrefix}\\{fileName}";

			yield return (file, fileName, rel);
		}

		if(!recursive)
			yield break;

		foreach((string subDirFullName, string subDirName) in dirs)
		{
			cancellationToken.ThrowIfCancellationRequested();

			onDirectoryEntered();

			string subPrefix = string.IsNullOrEmpty(relativePrefix)
				? subDirName
				: $"{relativePrefix}\\{subDirName}";

			await foreach((MediaFileInfo file, string fileName, string rel) in EnumerateRecursiveAsync(
				subDirFullName,
				subPrefix,
				recursive,
				onDirectoryEntered,
				cancellationToken).ConfigureAwait(false)
			)
			{
				yield return (file, fileName, rel);
			}
		}
	}

	private static DateTimeOffset? ToUtcOffsetOrNull(DateTime? value)
	{
		return value is DateTime dt
			? new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero)
			: null;
	}
}