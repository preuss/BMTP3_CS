namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;

/// <summary>
/// Defines a generic contract for scanning a source and producing raw entries.
/// </summary>
public interface ITraversalScanner<TEntry>
{
	/// <summary>
	/// Performs a traversal of the source and streams entries back to the caller.
	/// 
	/// Constructor parameters should only capture dependencies that are required
	/// for the scanner to exist (e.g. a MediaDevice instance). These define the
	/// identity of the scanner and do not change per operation.
	/// 
	/// Method parameters represent operational choices that can vary per call:
	/// - <paramref name="rootPath"/> specifies the starting point of traversal.
	///   It is not necessarily the root of the drive, but the root of the scan.
	/// - <paramref name="recursive"/> controls whether subdirectories are scanned.
	///   This can be toggled per operation.
	/// - <paramref name="progress"/> allows reporting of traversal snapshots
	///   (file count, directory count, last names, etc.) via IProgress.
	/// - <paramref name="cancellationToken"/> enables cooperative cancellation
	///   of long-running scans.
	/// 
	/// The method is synchronous and returns an <see cref="IEnumerable{TEntry}"/>
	/// that is lazy: entries are produced one at a time as the caller iterates.
	/// </summary>
	IEnumerable<TEntry> Scan(string rootPath, bool recursive = true, IProgress<TraversalProgress>? progress = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Performs a traversal of the source asynchronously and streams entries back
	/// to the caller using <see cref="IAsyncEnumerable{TEntry}"/>.
	/// 
	/// As with <see cref="Scan"/>, constructor parameters define dependencies
	/// (e.g. MediaDevice) while method parameters define operational choices.
	/// 
	/// - <paramref name="rootPath"/> is the starting point of traversal.
	/// - <paramref name="recursive"/> determines whether subdirectories are scanned.
	/// - <paramref name="progress"/> reports traversal snapshots via IProgress.
	/// - <paramref name="cancellationToken"/> allows cancellation of the operation.
	/// 
	/// The method is asynchronous and supports <c>await foreach</c>, making it
	/// suitable for UI or background scenarios where responsiveness matters.
	/// Entries are streamed lazily: each file or directory is produced only when
	/// the caller requests the next item.
	/// </summary>
	IAsyncEnumerable<TEntry> ScanAsync(string rootPath, bool recursive = true, IProgress<TraversalProgress>? progress = null, CancellationToken cancellationToken = default);

}
