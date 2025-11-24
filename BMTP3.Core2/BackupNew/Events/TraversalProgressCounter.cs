using System;
using System.Threading;

namespace BMTP3.Core2.BackupNew.Events;
public class TraversalProgressCounter
{
	private int _fileCount;
	private int _directoryCount;
	private readonly object _lock = new();

	private readonly SynchronizationContext _syncContext;

	public event EventHandler<int>? FileCountChanged;
	public event EventHandler<int>? DirectoryCountChanged;
	public event EventHandler<FileAndDirectoryCount>? CombinedCountChanged;

	public TraversalProgressCounter()
	{
		_syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
	}

	/// <summary>
	/// Increments the file count and raises relevant events.
	/// </summary>
	public void IncrementFileCount(int incrementWith = 1)
	{
		if(incrementWith == 0) return;

		FileAndDirectoryCount currentCount;
		lock(_lock)
		{
			_fileCount += incrementWith;
			currentCount = new(_fileCount, _directoryCount);
		}

		PostEvent(FileCountChanged, currentCount.FileCount);
		PostEvent(CombinedCountChanged, currentCount);
	}

	/// <summary>
	/// Increments the directory count and raises relevant events.
	/// </summary>
	public void IncrementDirectoryCount(int incrementWith = 1)
	{
		if(incrementWith == 0) return;

		FileAndDirectoryCount currentCount;
		lock(_lock)
		{
			_directoryCount += incrementWith;
			currentCount = new(_fileCount, _directoryCount);
		}

		PostEvent(DirectoryCountChanged, currentCount.DirectoryCount);
		PostEvent(CombinedCountChanged, currentCount);
	}

	/// <summary>
	/// Reports the current counts without incrementing.
	/// Useful for initial UI binding.
	/// </summary>
	public void ReportCurrent()
	{
		FileAndDirectoryCount currentCount;
		lock(_lock)
		{
			currentCount = new(_fileCount, _directoryCount);
		}

		PostEvent(FileCountChanged, currentCount.FileCount);
		PostEvent(DirectoryCountChanged, currentCount.DirectoryCount);
		PostEvent(CombinedCountChanged, currentCount);
	}

	/// <summary>
	/// Resets both counters to zero and reports the reset state.
	/// </summary>
	public void Reset()
	{
		FileAndDirectoryCount currentCount;
		lock(_lock)
		{
			_fileCount = 0;
			_directoryCount = 0;
			currentCount = new(_fileCount, _directoryCount);
		}

		PostEvent(FileCountChanged, currentCount.FileCount);
		PostEvent(DirectoryCountChanged, currentCount.DirectoryCount);
		PostEvent(CombinedCountChanged, currentCount);
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
/// Immutable snapshot of file and directory counts.
/// </summary>
public record FileAndDirectoryCount(int FileCount, int DirectoryCount);
