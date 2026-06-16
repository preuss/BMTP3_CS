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
/// <para>
/// Traversal acquires exclusive MTP device access (<see cref="IMediaDeviceGatekeeper"/>) for
/// the entire enumeration. Metadata properties (<c>CreationTime</c>, <c>LastWriteTime</c>,
/// <c>DateAuthored</c>) are captured during this phase while the gatekeeper is held.
/// <see cref="MediaDeviceContent"/> does not open streams during traversal; it acquires the
/// gatekeeper later when consumed (download phase).
/// </para>
/// <para>
/// <b>Deadlock warning:</b> The gatekeeper lease is held for the entire <c>TraverseAsync</c>
/// enumeration. Callers must not consume <see cref="MediaDeviceContent"/> streams (e.g.,
/// <c>OpenRead</c>) while actively iterating, because content access also acquires the
/// gatekeeper. Complete enumeration first, then consume the returned items.
/// </para>
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

		using IDisposable _ = await _gatekeeper.AcquireAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);

		IMediaDirectory rootDirectory = _mediaDrive.RootDirectory
			?? throw new InvalidOperationException("Drive root directory is not available.");

		string normalizedSubPath = NormalizeMtpRelativePath(request.SubPath ?? "");
		IReadOnlyList<string>? includePatterns = NormalizeMtpGlobPatterns(request.IncludePatterns);
		IReadOnlyList<string>? excludePatterns = NormalizeMtpGlobPatterns(request.ExcludePatterns);

		IMediaDirectory startDirectory = string.IsNullOrEmpty(normalizedSubPath)
			? rootDirectory
			: NavigateToSubDirectory(rootDirectory, normalizedSubPath, cancellationToken);

		int dirCount = 1; // start directory; subdirectories are counted in onSubDirectoryEntered
		int fileCount = 0;

		progress?.Report(new SourceTraversalProgress
		{
			DirectoriesTraversed = dirCount,
			FilesDiscovered = fileCount,
		});

		foreach((IMediaFile file, string fileName, string relativeFilePath) in EnumerateRecursive(
				startDirectory,
				relativePrefix: "",
				recursive: request.Recursive,
				onSubDirectoryEntered: () =>
				{
					dirCount++;
					progress?.Report(new SourceTraversalProgress
					{
						DirectoriesTraversed = dirCount,
						FilesDiscovered = fileCount,
					});
				}, cancellationToken
			)
		)
		{
			cancellationToken.ThrowIfCancellationRequested();

			fileCount++;

			progress?.Report(new SourceTraversalProgress
			{
				DirectoriesTraversed = dirCount,
				FilesDiscovered = fileCount,
			});

			// Already normalised in EnumerateRecursive, but normalise again for safety
			// before glob matching and SourcePath construction.
			string normalizedPath = NormalizeMtpRelativePath(relativeFilePath);

			if(!GlobMatcher.IsIncluded(normalizedPath, includePatterns, excludePatterns))
				continue;

			SourceTraversalItem item = new()
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
				SourcePath = BuildMtpSourcePath(_mediaDevice.FriendlyName, _mediaDrive.Name, normalizedSubPath, normalizedPath),
				RelativeFilePath = normalizedPath,
				FileName = fileName,
				Content = new MediaDeviceContent(file, _gatekeeper),
				DateCreated = ToUtcOffsetOrNull(file.CreationTime),
				DateModified = ToUtcOffsetOrNull(file.LastWriteTime),
				DateAuthored = ToUtcOffsetOrNull(file.DateAuthored),
				DateAccessed = null,
			};

			yield return item;
		}
	}

	private static IEnumerable<(IMediaFile File, string FileName, string RelativeFilePath)> EnumerateRecursive(
		IMediaDirectory directory,
		string relativePrefix,
		bool recursive,
		Action onSubDirectoryEntered,
		CancellationToken cancellationToken
	)
	{
		foreach(IMediaFile file in directory.EnumerateFiles())
		{
			cancellationToken.ThrowIfCancellationRequested();

			string rel = CombineMtpRelativePath(relativePrefix, file.Name);
			rel = NormalizeMtpRelativePath(rel);

			yield return (file, file.Name, rel);
		}

		if(!recursive)
			yield break;

		foreach(IMediaDirectory subDir in directory.EnumerateDirectories())
		{
			cancellationToken.ThrowIfCancellationRequested();

			onSubDirectoryEntered();

			string subPrefix = CombineMtpRelativePath(relativePrefix, subDir.Name);
			subPrefix = NormalizeMtpRelativePath(subPrefix);

			foreach((IMediaFile file, string fileName, string rel) in EnumerateRecursive(
				subDir,
				subPrefix,
				recursive,
				onSubDirectoryEntered,
				cancellationToken
			))
			{
				yield return (file, fileName, rel);
			}
		}
	}

	private static string CombineMtpRelativePath(string prefix, string name)
	{
		return string.IsNullOrEmpty(prefix)
			? name
			: $"{prefix}\\{name}";
	}

	private static IMediaDirectory NavigateToSubDirectory(
		IMediaDirectory root,
		string subPath,
		CancellationToken cancellationToken
	)
	{
		string[] segments = subPath.Split('/', '\\');

		IMediaDirectory current = root;

		foreach(string segment in segments)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if(string.IsNullOrWhiteSpace(segment))
				continue;

			IMediaDirectory? next = null;

			foreach(IMediaDirectory dir in current.EnumerateDirectories())
			{
				cancellationToken.ThrowIfCancellationRequested();

				if(string.Equals(dir.Name, segment, StringComparison.OrdinalIgnoreCase))
				{
					next = dir;
					break;
				}
			}

			if(next == null)
			{
				throw new DirectoryNotFoundException($"Directory '{segment}' not found in '{current.Name}' while navigating to '{subPath}'.");
			}

			current = next;
		}

		return current;
	}

	private static DateTimeOffset? ToUtcOffsetOrNull(DateTime? value)
	{
		if(value == null) return null;

		DateTime dateTime = value.Value;

		return dateTime.Kind switch
		{
			DateTimeKind.Utc => new DateTimeOffset(dateTime, TimeSpan.Zero),
			DateTimeKind.Local => dateTime.ToUniversalTime(),
			DateTimeKind.Unspecified => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Local)).ToUniversalTime(),
			_ => null,
		};
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
	/// Within a single scan session, <see cref="IMediaItem.PersistentUniqueId"/> is guaranteed
	/// unique by the WPD driver and is the
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
		ArgumentException.ThrowIfNullOrWhiteSpace(fullFilePath);

		return $"{fullFilePath}_{size}_{dateCreated?.UtcTicks}_{dateModified?.UtcTicks}_{dateAuthored?.UtcTicks}";
	}

	/// <summary>
	/// Builds a logical MTP source URL for display and external identification.
	/// Internal MTP relative paths use <c>\</c>; this URL uses <c>/</c>.
	/// Not a standards-compliant URI — device, drive, and path segments are not URI-escaped.
	/// </summary>
	internal static string BuildMtpSourcePath(string deviceName, string driveName, string? subPath, string relativeFilePath)
	{
		string normalizedRel = relativeFilePath.Replace('\\', '/').Trim('/');
		string normalizedSub = subPath?.Replace('\\', '/').Trim('/') ?? "";

		if(string.IsNullOrEmpty(normalizedSub))
			return $"mtp://{deviceName}/{driveName}/{normalizedRel}";

		return $"mtp://{deviceName}/{driveName}/{normalizedSub}/{normalizedRel}";
	}

	/// <summary>
	/// Normalizes an MTP relative path to use <c>\</c> consistently.
	/// </summary>
	private static string NormalizeMtpRelativePath(string path)
	{
		ArgumentNullException.ThrowIfNull(path);

		return path.Replace('/', '\\').Trim('\\');
	}

	/// <summary>
	/// Normalizes glob patterns to use <c>\</c> consistently with MTP relative paths.
	/// </summary>
	private static IReadOnlyList<string>? NormalizeMtpGlobPatterns(IReadOnlyList<string>? patterns)
	{
		if(patterns == null)
			return null;

		if(patterns.Count == 0)
			return Array.Empty<string>();

		string[] result = new string[patterns.Count];

		for(int i = 0; i < patterns.Count; i++)
		{
			result[i] = NormalizeMtpRelativePath(patterns[i]);
		}

		return result;
	}
}
