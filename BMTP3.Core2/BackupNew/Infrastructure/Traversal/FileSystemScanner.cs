using BMTP3.Core2.BackupNew.Engine.Traversal;
using System.Runtime.CompilerServices;

namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;
/// <summary>
/// Traverses a local file system and streams <see cref="FileInfo"/> entries.
/// Implements <see cref="ITraversalScanner{TEntry}"/> with FileInfo as the entry type.
/// </summary>
public sealed class FileSystemScanner : ITraversalScanner<FileInfo>
{
	/// <summary>
	/// Performs a traversal of the file system starting at <paramref name="rootPath"/>.
	/// 
	/// Constructor parameters should only capture dependencies required for the scanner to exist.
	/// Method parameters represent operational choices that can vary per call:
	/// - <paramref name="rootPath"/> specifies the starting point of traversal.
	/// - <paramref name="recursive"/> controls whether subdirectories are scanned.
	/// - <paramref name="progress"/> allows reporting of traversal snapshots via IProgress.
	/// - <paramref name="cancellationToken"/> enables cooperative cancellation.
	/// 
	/// The method is synchronous and returns an <see cref="IEnumerable{FileInfo}"/> that is lazy:
	/// entries are produced one at a time as the caller iterates.
	/// </summary>
	public IEnumerable<FileInfo> Scan(string rootPath, bool recursive = true, IProgress<TraversalProgress>? progress = null, CancellationToken cancellationToken = default)
	{
		return ScanAsync(rootPath, recursive, progress, cancellationToken)
			.ToBlockingEnumerable(cancellationToken);
	}

	/// <summary>
	/// Performs a traversal of the file system asynchronously starting at <paramref name="rootPath"/>.
	/// 
	/// As with <see cref="Scan"/>, constructor parameters define dependencies while method parameters
	/// define operational choices. The method supports <c>await foreach</c> and streams entries lazily.
	/// </summary>
	public async IAsyncEnumerable<FileInfo> ScanAsync(string rootPath, bool recursive = true, IProgress<TraversalProgress>? progress = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		DirectoryInfo dirInfo = new(rootPath);

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
			dirInfo, recursive,
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

	private async IAsyncEnumerable<FileInfo> ScanInternalAsync(DirectoryInfo dir, bool recursive, Action<FileInfo> onFile, Action<DirectoryInfo> onDirectory, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		foreach(var file in SafeGetFiles(dir))
		{
			cancellationToken.ThrowIfCancellationRequested();

			onFile(file);
			yield return file;

			// Cooperative scheduling in long traversals
			await Task.Yield();
		}

		if(recursive)
		{
			foreach(var subDir in SafeGetDirectories(dir))
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

	private static IEnumerable<FileInfo> SafeGetFiles(DirectoryInfo dir)
	{
		try { return dir.GetFiles(); } catch { return Array.Empty<FileInfo>(); }
	}

	private static IEnumerable<DirectoryInfo> SafeGetDirectories(DirectoryInfo dir)
	{
		try { return dir.GetDirectories(); } catch { return Array.Empty<DirectoryInfo>(); }
	}
}