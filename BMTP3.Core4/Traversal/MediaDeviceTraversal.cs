using BMTP3.Common.Utilities;
using BMTP3.Core4.Api.Models.Enums;
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
    private static readonly TimeSpan GatekeeperAcquireTimeout = TimeSpan.FromSeconds(30);

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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using IDisposable _ = await _gatekeeper.AcquireAsync(GatekeeperAcquireTimeout, cancellationToken).ConfigureAwait(false);

        IMediaDirectory rootDirectory = _mediaDrive.RootDirectory
            ?? throw new InvalidOperationException("Drive root directory is not available.");

        string normalizedSubPath = PathHelper.NormalizePath(request.SubPath ?? "");
        IReadOnlyList<string>? includePatterns = PathHelper.NormalizePatterns(request.IncludePatterns);
        IReadOnlyList<string>? excludePatterns = PathHelper.NormalizePatterns(request.ExcludePatterns);

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

        foreach ((IMediaFile file, string fileName, string relativeFilePath) in EnumerateRecursive(
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
            },
            cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            fileCount++;
            progress?.Report(new SourceTraversalProgress
            {
                DirectoriesTraversed = dirCount,
                FilesDiscovered = fileCount,
            });

            string normalizedPath = PathHelper.NormalizePath(relativeFilePath);
            string relativePathForGlobMatching = normalizedPath.Replace('\\', '/');

            if(!GlobMatcher.IsIncluded(relativePathForGlobMatching, includePatterns, excludePatterns, GlobSeparatorMode.ForwardSlash))
                continue;

            DateTimeOffset? dateCreated = ToUtcOffsetOrNull(file.CreationTime);
            DateTimeOffset? dateModified = ToUtcOffsetOrNull(file.LastWriteTime);
            DateTimeOffset? dateAuthored = ToUtcOffsetOrNull(file.DateAuthored);

            string itemId = request.ItemIdScope switch
            {
                ItemIdScope.Session    => Guard.RequireNonNull(file.Id),
                ItemIdScope.Connection => Guard.RequireNonNull(file.PersistentUniqueId),
                ItemIdScope.Persistent => GenerateDeviceUniqueId(
                    file.FullName, file.Length, dateCreated, dateModified, dateAuthored),
                _ => Guard.RequireNonNull(file.PersistentUniqueId),
            };

            yield return new SourceTraversalItem
            {
                Id = itemId,
                SourcePath = BuildMtpSourcePath(_mediaDevice.FriendlyName, _mediaDrive.Name, normalizedSubPath, normalizedPath),
                RelativeFilePath = normalizedPath,
                FileName = fileName,
                Content = new MediaDeviceContent(file, _gatekeeper),
                DateCreated = dateCreated,
                DateModified = dateModified,
                DateAuthored = dateAuthored,
                DateAccessed = null,
            };
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
        foreach (IMediaFile file in directory.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string rel = CombineMtpRelativePath(relativePrefix, file.Name);

            yield return (file, file.Name, rel);
        }

        if (!recursive)
            yield break;

        foreach (IMediaDirectory subDir in directory.EnumerateDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();

            onSubDirectoryEntered();

            string subPrefix = CombineMtpRelativePath(relativePrefix, subDir.Name);

            foreach ((IMediaFile file, string fileName, string rel) in EnumerateRecursive(
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
        if (string.IsNullOrEmpty(prefix)) return name;
        if (string.IsNullOrEmpty(name))   return prefix;
        return $"{prefix}\\{name}";
    }

    private static IMediaDirectory NavigateToSubDirectory(
        IMediaDirectory root,
        string subPath,
        CancellationToken cancellationToken
    )
    {
        IMediaDirectory current = root;

        foreach (string segment in SplitMtpRelativePath(subPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            IMediaDirectory? next = null;

            foreach (IMediaDirectory dir in current.EnumerateDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.Equals(dir.Name, segment, StringComparison.OrdinalIgnoreCase))
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

    private static IEnumerable<string> SplitMtpRelativePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        foreach (string segment in path.Split('\\'))
        {
            if (segment.Length > 0)
                yield return segment;
        }
    }

    private static DateTimeOffset? ToUtcOffsetOrNull(DateTime? value)
    {
        if (value is not { } dateTime)
            return null;

        return dateTime.Kind switch
        {
            DateTimeKind.Utc         => new DateTimeOffset(dateTime, TimeSpan.Zero),
            DateTimeKind.Local       => new DateTimeOffset(dateTime.ToUniversalTime(), TimeSpan.Zero),
            DateTimeKind.Unspecified => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Local)).ToUniversalTime(),
            _                        => throw new ArgumentOutOfRangeException(nameof(value), $"Unexpected DateTimeKind: {dateTime.Kind}"),
        };
    }

    /// <summary>
    /// Generates a device-unique identifier for an MTP file by combining its
    /// full device path with file size and all available timestamps.
    /// Stable across sessions and physical USB reconnections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Neither <c>PersistentUniqueId</c> nor <c>FullName</c> can be guaranteed stable across
    /// device reconnections — especially on Apple devices. This method composes a content-derived
    /// identifier whose combination of five fields makes collisions extraordinarily unlikely.
    /// </para>
    /// <para>
    /// Within a single session, <see cref="IMediaItem.PersistentUniqueId"/> remains the correct
    /// primary identifier. This generated Id serves cross-connection resume scenarios.
    /// </para>
    /// </remarks>
    internal static string GenerateDeviceUniqueId(
        string fullFilePath,
        ulong size,
        DateTimeOffset? dateCreated,
        DateTimeOffset? dateModified,
        DateTimeOffset? dateAuthored)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullFilePath);

        // Bump version if the algorithm changes in a non-backwards-compatible way.
        const string prefix = "mtp-stable-v1";

        return $"{prefix}_{fullFilePath}_{size}_{dateCreated?.UtcTicks}_{dateModified?.UtcTicks}_{dateAuthored?.UtcTicks}";
    }

    /// <summary>
    /// Builds a logical MTP source URL for display and external identification.
    /// Internal MTP relative paths use <c>\</c>; this URL uses <c>/</c>.
    /// Not a standards-compliant URI — segments are not URI-escaped.
    /// </summary>
    internal static string BuildMtpSourcePath(string deviceName, string driveName, string? subPath, string relativeFilePath)
    {
        string normalizedRel = relativeFilePath.Replace('\\', '/').Trim('/');
        string normalizedSub = subPath?.Replace('\\', '/').Trim('/') ?? "";

        if (string.IsNullOrEmpty(normalizedSub))
            return $"mtp://{deviceName}/{driveName}/{normalizedRel}";

        return $"mtp://{deviceName}/{driveName}/{normalizedSub}/{normalizedRel}";
    }
}