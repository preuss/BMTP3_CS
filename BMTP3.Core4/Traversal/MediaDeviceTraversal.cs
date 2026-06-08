using BMTP3.Core4.Devices;
using BMTP3.Core4.Models;
using BMTP3.Core4.Storage;
using BMTP3.Core4.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace BMTP3.Core4.Traversal;

/// <summary>
/// Traverses content on an MTP device.
/// </summary>
/// <remarks>
/// This type does not own the lifetime of the underlying <see cref="IMediaDevice"/>.
/// Yielded <see cref="SourceTraversalItem"/> instances may contain <see cref="MediaDeviceContent"/>
/// objects that continue to access the device after traversal has finished.
/// The caller must therefore keep the device alive until all yielded content and streams
/// have been fully consumed.
/// </remarks>
[SupportedOSPlatform("windows7.0")]

internal sealed class MediaDeviceTraversal : ISourceTraversal
{
	private readonly IMediaDevice _mediaDevice;
	private readonly IMediaDrive _mediaDrive;
	private readonly IMediaDeviceGatekeeper _gatekeeper;

	public MediaDeviceTraversal(IConnectedMediaDriveSource mediaDriveSource, IMediaDeviceGatekeeper gatekeeper)
	{
		ArgumentNullException.ThrowIfNull(mediaDriveSource);
		_mediaDevice = mediaDriveSource.Device ?? throw new ArgumentNullException(nameof(mediaDriveSource.Device));
		_mediaDrive = mediaDriveSource.Drive ?? throw new ArgumentNullException(nameof(mediaDriveSource.Drive));
		_gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	public async IAsyncEnumerable<SourceTraversalItem> TraverseAsync(
		SourceTraversalRequest request,
		IProgress<SourceTraversalProgress>? progress,
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
	{
		ArgumentNullException.ThrowIfNull(request);

		IMediaDirectory rootDirectory = _mediaDrive.RootDirectory
			?? throw new InvalidOperationException("Drive root directory is not available.");

		int dirCount = 0;
		int fileCount = 0;

		await foreach ((IMediaFile file, string fileName, string relativePath) in EnumerateRecursiveAsync(
						  rootDirectory,
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

			if (!GlobMatcher.IsIncluded(relativePath, request.IncludePatterns, request.ExcludePatterns))
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
					DateAccessed = null,
				};

				return Task.FromResult(result);
			}, cancellationToken).ConfigureAwait(false);

			yield return item;
		}
	}

	private async IAsyncEnumerable<(IMediaFile File, string FileName, string RelativePath)> EnumerateRecursiveAsync(
		IMediaDirectory directory,
		string relativePrefix,
		bool recursive,
		Action onDirectoryEntered,
		[EnumeratorCancellation] CancellationToken cancellationToken
	)
	{
		(List<(IMediaFile File, string Name)> files, List<(IMediaDirectory Dir, string Name)> dirs) = await _gatekeeper.ExecuteAsync(_ =>
		{
			List<(IMediaFile File, string Name)> files = directory.Files
				.Select(f => (f, f.Name))
				.ToList();

			List<(IMediaDirectory Dir, string Name)> dirs = recursive
				? directory.Directories
					.Select(d => (d, d.Name))
					.ToList()
				: new();

			return Task.FromResult((files, dirs));
		}, cancellationToken).ConfigureAwait(false);

		foreach ((IMediaFile file, string fileName) in files)
		{
			cancellationToken.ThrowIfCancellationRequested();

			string rel = string.IsNullOrEmpty(relativePrefix)
				? fileName
				: $"{relativePrefix}\\{fileName}";

			yield return (file, fileName, rel);
		}

		if (!recursive)
			yield break;

		foreach ((IMediaDirectory subDir, string subDirName) in dirs)
		{
			cancellationToken.ThrowIfCancellationRequested();

			onDirectoryEntered();

			string subPrefix = string.IsNullOrEmpty(relativePrefix)
				? subDirName
				: $"{relativePrefix}\\{subDirName}";

			await foreach ((IMediaFile file, string fileName, string rel) in EnumerateRecursiveAsync(
					subDir,
					subPrefix,
					recursive,
					onDirectoryEntered,
					cancellationToken
				).ConfigureAwait(false)
			)
			{
				yield return (file, fileName, rel);
			}
		}
	}

	private static DateTimeOffset? ToUtcOffsetOrNull(DateTime? value)
	{
		return value != null
			? new DateTimeOffset(value.Value.ToUniversalTime(), TimeSpan.Zero)
			: null;
	}
}
