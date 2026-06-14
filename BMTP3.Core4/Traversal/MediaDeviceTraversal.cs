using BMTP3.Common.Utilities;
using BMTP3.Core4.Devices;
using BMTP3.Core4.Helpers;
using BMTP3.Core4.Models;
using BMTP3.Core4.Storage;
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

		IMediaDirectory startDirectory = string.IsNullOrEmpty(request.SubPath)
			? rootDirectory
			: await NavigateToSubDirectory(rootDirectory, request.SubPath, cancellationToken).ConfigureAwait(false);

		int dirCount = 0;
		int fileCount = 0;

		await foreach((IMediaFile file, string fileName, string relativeFilePath) in EnumerateRecursiveAsync(
				startDirectory,
				relativePrefix: "",
				recursive: request.Recursive,
				onDirectoryEntered: () => dirCount++, cancellationToken
			).ConfigureAwait(false)
		)
		{
			cancellationToken.ThrowIfCancellationRequested();

			fileCount++;

			progress?.Report(new SourceTraversalProgress
			{
				DirectoriesTraversed = dirCount,
				FilesDiscovered = fileCount,
			});

			if(!GlobMatcher.IsIncluded(relativeFilePath, request.IncludePatterns, request.ExcludePatterns))
				continue;

			SourceTraversalItem item = await _gatekeeper.ExecuteAsync(_ =>
			{
				DateTimeOffset? created = ToUtcOffsetOrNull(file.CreationTime);
				DateTimeOffset? modified = ToUtcOffsetOrNull(file.LastWriteTime);
				DateTimeOffset? authored = ToUtcOffsetOrNull(file.DateAuthored);

				SourceTraversalItem result = new()
				{
					// WPD object ID — guaranteed unique within a single scan session.
					// NOT stable across device reconnections (WPD may reassign IDs).
					// Apple devices does not respect PersistentUniqueId between connections or restarts of device.
					// But we use PersistentUniqueId for them anyway, because it is the only
					// option that should provide stability across reconnections and restarts.
					// For non-Apple devices it actually works as intended.
					//
					// For true cross-connection matching, see GenerateAlmostUniqueId() which
					// combines path + size + timestamps as a future fallback strategy.
					Id = Guard.RequireNonNull(file.PersistentUniqueId),
					SourcePath = BuildMtpSourcePath(_mediaDevice.FriendlyName, _mediaDrive.Name, request.SubPath, relativeFilePath),
					RelativeFilePath = relativeFilePath,
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

	private async IAsyncEnumerable<(IMediaFile File, string FileName, string RelativeFilePath)> EnumerateRecursiveAsync(
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

		foreach((IMediaFile file, string fileName) in files)
		{
			cancellationToken.ThrowIfCancellationRequested();

			string rel = string.IsNullOrEmpty(relativePrefix)
				? fileName
				: $"{relativePrefix}\\{fileName}";

			yield return (file, fileName, rel);
		}

		if(!recursive)
			yield break;

		foreach((IMediaDirectory subDir, string subDirName) in dirs)
		{
			cancellationToken.ThrowIfCancellationRequested();

			onDirectoryEntered();

			string subPrefix = string.IsNullOrEmpty(relativePrefix)
				? subDirName
				: $"{relativePrefix}\\{subDirName}";

			await foreach((IMediaFile file, string fileName, string rel) in EnumerateRecursiveAsync(
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

	private Task<IMediaDirectory> NavigateToSubDirectory(
		IMediaDirectory root,
		string subPath,
		CancellationToken cancellationToken
	)
	{
		string[] segments = subPath.Split('/', '\\');

		return _gatekeeper.ExecuteAsync(_ =>
		{
			IMediaDirectory current = root;

			foreach(string segment in segments)
			{
				if(string.IsNullOrWhiteSpace(segment))
					continue;

				IMediaDirectory? next = current.Directories
					.FirstOrDefault(d => string.Equals(d.Name, segment, StringComparison.OrdinalIgnoreCase));

				if(next == null)
				{
					throw new DirectoryNotFoundException(
						$"Directory '{segment}' not found in '{current.Name}' while navigating to '{subPath}'.");
				}

				current = next;
			}

			return Task.FromResult(current);
		}, cancellationToken);
	}

	private static DateTimeOffset? ToUtcOffsetOrNull(DateTime? value)
	{
		return value != null
			? new DateTimeOffset(value.Value.ToUniversalTime(), TimeSpan.Zero)
			: null;
	}

	/// <summary>
	/// Generates an "almost unique" identifier for an MTP file by combining its
	/// full device path with file size and all available timestamps.
	/// </summary>
	/// <remarks>
	/// <para><b>Why this exists:</b></para>
	/// <para>
	/// Neither <c>PersistentUniqueId</c> (WPD object GUID) nor <c>FullName</c> (WPD device path)
	/// can be guaranteed stable across device reconnections — especially on Apple devices,
	/// where both may be regenerated on every reconnect or device restart.
	/// This makes cross-connection file matching fundamentally unreliable using WPD identifiers alone.
	/// </para>
	/// <para>
	/// This method composes a content-derived identifier from the file's device path, size,
	/// and up to three timestamps. The combination of all five fields makes collisions
	/// extraordinarily unlikely in practice, even though no strict uniqueness guarantee exists.
	/// </para>
	/// <para><b>Why it is not used as the primary Id:</b></para>
	/// <para>
	/// Within a single scan session, <see cref="IMediaItem.PersistentUniqueId"/> (with fallback
	/// to <see cref="IMediaItem.Id"/>) is guaranteed unique by the WPD driver and is the
	/// correct identifier for deduplication and record keeping within that session.
	/// </para>
	/// <para><b>Future use:</b></para>
	/// <para>
	/// This method exists as a foundation for cross-connection resume and file matching.
	/// When time permits, the session resume logic (<c>SessionStateService</c>) should be
	/// extended to fall back to this generated identifier when <c>PersistentUniqueId</c>
	/// matching fails — enabling best-effort file re-identification across MTP reconnections.
	/// </para>
	/// </remarks>
	internal static string GenerateAlmostUniqueId(
		string fullFilePath,
		ulong size,
		DateTimeOffset? dateCreated,
		DateTimeOffset? dateModified,
		DateTimeOffset? dateAuthored
	)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(fullFilePath);
		ArgumentNullException.ThrowIfNull(size);

		return $"{fullFilePath}_{size}_{dateCreated?.Ticks}_{dateModified?.Ticks}_{dateAuthored?.Ticks}";
	}

	internal static string BuildMtpSourcePath(string deviceName, string driveName, string? subPath, string relativeFilePath)
	{
		string normalizedRel = relativeFilePath.Replace('\\', '/');
		string normalizedSub = subPath?.Replace('\\', '/') ?? "";

		if (string.IsNullOrEmpty(normalizedSub))
			return $"mtp://{deviceName}/{driveName}/{normalizedRel}";

		return $"mtp://{deviceName}/{driveName}/{normalizedSub}/{normalizedRel}";
	}
}
