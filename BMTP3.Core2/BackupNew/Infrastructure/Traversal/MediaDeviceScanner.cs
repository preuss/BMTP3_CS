using MediaDevices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;

[SupportedOSPlatform("windows7.0")]

/// <summary>
/// Traverses a MediaDevice and streams <see cref="MediaFileInfo"/> entries.
/// Implements <see cref="ITraversalScanner{TEntry}"/> with MediaFileInfo as the entry type.
/// </summary>
public sealed class MediaDeviceScanner : ITraversalScanner<MediaFileInfo>
{
	private readonly MediaDevice _device;
    private readonly IMtpGatekeeper _gatekeeper;

	/// <summary>
	/// Initializes a new scanner bound to a specific MediaDevice.
	/// The device is a required dependency and must be provided via constructor.
	/// </summary>
	public MediaDeviceScanner(MediaDevice device, IMtpGatekeeper gatekeeper)
	{
		ArgumentNullException.ThrowIfNull(device);
		_device = device;
        _gatekeeper = gatekeeper ?? throw new ArgumentNullException(nameof(gatekeeper));
	}

	/// <summary>
	/// Performs a traversal of the media device starting at <paramref name="rootPath"/>.
	/// 
	/// Constructor parameters capture dependencies (MediaDevice).
	/// Method parameters represent operational choices:
	/// - <paramref name="rootPath"/> specifies the starting point of traversal.
	/// - <paramref name="recursive"/> controls whether subdirectories are scanned.
	/// - <paramref name="progress"/> allows reporting of traversal snapshots via IProgress.
	/// - <paramref name="cancellationToken"/> enables cooperative cancellation.
	/// 
	/// The method is synchronous and returns an <see cref="IEnumerable{MediaFileInfo}"/> that is lazy.
	/// </summary>
	public IEnumerable<MediaFileInfo> Scan(string rootPath, bool recursive = true, IProgress<TraversalProgress>? progress = null, CancellationToken cancellationToken = default)
	{
		return ScanAsync(rootPath, recursive, progress, cancellationToken)
			.ToBlockingEnumerable(cancellationToken);
	}

	/// <summary>
	/// Performs a traversal of the media device asynchronously starting at <paramref name="rootPath"/>.
	/// Supports <c>await foreach</c> and streams entries lazily.
	/// </summary>
	public async IAsyncEnumerable<MediaFileInfo> ScanAsync(string rootPath, bool recursive = true, IProgress<TraversalProgress>? progress = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if(!_device.DirectoryExists(rootPath))
		{
			yield break;
		}

		MediaDirectoryInfo rootDir = _device.GetDirectoryInfo(rootPath);

		// Single snapshot object, updated with 'with' each time
		TraversalProgress snapshot = new(
			FileCount: 0,
			LastFileName: null,
			FileCountChanged: false,
			DirectoryCount: 0,
			LastDirectoryName: null,
			DirectoryCountChanged: false
		);

		await foreach(var file in ScanInternalAsync(
			rootDir, recursive,
			onFile: f =>
			{
				snapshot = snapshot with
				{
					FileCount = snapshot.FileCount + 1,
					LastFileName = f.Name,
					FileCountChanged = true,
					DirectoryCountChanged = false
				};
				progress?.Report(snapshot);
			},
			onDirectory: d =>
			{
				snapshot = snapshot with
				{
					DirectoryCount = snapshot.DirectoryCount + 1,
					LastDirectoryName = d.Name,
					FileCountChanged = false,
					DirectoryCountChanged = true
				};
				progress?.Report(snapshot);
			},
			cancellationToken
		))
		{
			yield return file;
		}
	}

	private async IAsyncEnumerable<MediaFileInfo> ScanInternalAsync(MediaDirectoryInfo dir, bool recursive, Action<MediaFileInfo> onFile, Action<MediaDirectoryInfo> onDirectory, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
        // Wrap EnumerateFiles in Gatekeeper and materialize list to keep lock time short
        var files = await _gatekeeper.ExecuteAsync(() => Task.FromResult(SafeEnumerateFiles(dir).ToList()), cancellationToken);

		foreach(var file in files)
		{
			cancellationToken.ThrowIfCancellationRequested();

			onFile(file);
			yield return file;

			// Cooperative scheduling in long traversals
			await Task.Yield();
		}

		if(recursive)
		{
            // Wrap EnumerateDirectories in Gatekeeper
            var subDirs = await _gatekeeper.ExecuteAsync(() => Task.FromResult(SafeEnumerateDirectories(dir).ToList()), cancellationToken);

			foreach(var subDir in subDirs)
			{
				cancellationToken.ThrowIfCancellationRequested();

				onDirectory(subDir);

				await foreach(var f in ScanInternalAsync(subDir, recursive, onFile, onDirectory, cancellationToken))
				{
					yield return f;
				}
			}
		}
	}

	private static IEnumerable<MediaFileInfo> SafeEnumerateFiles(MediaDirectoryInfo dir)
	{
		try { return dir.EnumerateFiles(); } catch { return Array.Empty<MediaFileInfo>(); }
	}

	private static IEnumerable<MediaDirectoryInfo> SafeEnumerateDirectories(MediaDirectoryInfo dir)
	{
		try { return dir.EnumerateDirectories(); } catch { return Array.Empty<MediaDirectoryInfo>(); }
	}
}
