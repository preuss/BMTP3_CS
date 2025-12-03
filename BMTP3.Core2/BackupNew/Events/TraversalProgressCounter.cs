namespace BMTP3.Core2.BackupNew.Events;
public class TraversalProgressCounter
{
	private int _fileCount;
	private int _directoryCount;
	private readonly object _lock = new();

	private readonly SynchronizationContext _syncContext;

	public event EventHandler<int>? FileCountChanged;
	public event EventHandler<int>? DirectoryCountChanged;
	public event EventHandler<ProgressSnapshotCount>? CombinedCountChanged;

	/// <summary>
	/// Creates a counter using the current synchronization context.
	/// </summary>
	public TraversalProgressCounter()
	{
		_syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
	}

	/// <summary>
	/// Atomically gets a snapshot of both counts.
	/// </summary>
	public ProgressSnapshotCount GetSnapshot()
	{
		lock(_lock)
		{
			return new ProgressSnapshotCount(_fileCount, _directoryCount);
		}
	}

	/// <summary>
	/// Increments the file count and raises relevant events.
	/// </summary>
	public void IncrementFileCount(int incrementWith = 1)
	{
		if(incrementWith == 0) return;
		if(incrementWith < 0) throw new ArgumentOutOfRangeException(nameof(incrementWith), "Negative increments are not allowed.");

		ProgressSnapshotCount snapshot;
		lock(_lock)
		{
			_fileCount += incrementWith;
			snapshot = new ProgressSnapshotCount(_fileCount, _directoryCount);
		}

		PostEvent(FileCountChanged, snapshot.FileCount);
		PostEvent(CombinedCountChanged, snapshot);
	}

	/// <summary>
	/// Increments the directory count and raises relevant events.
	/// </summary>
	public void IncrementDirectoryCount(int incrementWith = 1)
	{
		if(incrementWith == 0) return;
		if(incrementWith < 0) throw new ArgumentOutOfRangeException(nameof(incrementWith), "Negative increments are not allowed.");

		ProgressSnapshotCount snapshot;
		lock(_lock)
		{
			_directoryCount += incrementWith;
			snapshot = new ProgressSnapshotCount(_fileCount, _directoryCount);
		}

		PostEvent(DirectoryCountChanged, snapshot.DirectoryCount);
		PostEvent(CombinedCountChanged, snapshot);
	}

	/// <summary>
	/// Sets both counts explicitly and raises change events (useful for restore scenarios).
	/// </summary>
	private void SetCounts(int fileCount, int directoryCount)
	{
		if(fileCount < 0) throw new ArgumentOutOfRangeException(nameof(fileCount));
		if(directoryCount < 0) throw new ArgumentOutOfRangeException(nameof(directoryCount));
		ProgressSnapshotCount snapshot;
		lock(_lock)
		{
			_fileCount = fileCount;
			_directoryCount = directoryCount;
			snapshot = new ProgressSnapshotCount(_fileCount, _directoryCount);
		}
		PostEvent(FileCountChanged, snapshot.FileCount);
		PostEvent(DirectoryCountChanged, snapshot.DirectoryCount);
		PostEvent(CombinedCountChanged, snapshot);
	}

	/// <summary>
	/// Reports the current counts without incrementing.
	/// Useful for initial UI binding.
	/// </summary>
	public void ReportCurrent()
	{
		var snapshot = GetSnapshot();
		PostEvent(FileCountChanged, snapshot.FileCount);
		PostEvent(DirectoryCountChanged, snapshot.DirectoryCount);
		PostEvent(CombinedCountChanged, snapshot);
	}

	/// <summary>
	/// Resets both counters to zero and reports the reset state.
	/// </summary>
	public void Reset()
	{
		SetCounts(0, 0);
	}

	/// <summary>
	/// Posts an event to the synchronization context.
	/// Uses a static lambda to avoid closure allocations.
	/// </summary>
	private void PostEvent<T>(EventHandler<T>? handler, T value)
	{
		if(handler != null)
		{
			_syncContext.Post(static state =>
			{
				var (h, v, sender) = ((EventHandler<T>, T, TraversalProgressCounter))state!;
				h(sender, v);
			}, (handler, value, this));
		}
	}
}

/// <summary>
/// Immutable snapshot of file and directory counts at a given progress state.
/// </summary>
public readonly struct ProgressSnapshotCount
{
	/// <summary>
	/// Gets the number of files in the snapshot.
	/// </summary>
	public int FileCount { get; }

	/// <summary>
	/// Gets the number of directories in the snapshot.
	/// </summary>
	public int DirectoryCount { get; }

	/// <summary>
	/// Initializes a new immutable snapshot with the specified file and directory counts.
	/// </summary>
	/// <param name="fileCount">Number of files.</param>
	/// <param name="directoryCount">Number of directories.</param>
	public ProgressSnapshotCount(int fileCount, int directoryCount)
	{
		FileCount = fileCount;
		DirectoryCount = directoryCount;
	}
}
