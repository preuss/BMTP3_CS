using System;
using System.Threading;

namespace BMTP3.Core2.BackupNew.Events;
public class TraversalProgressCounter
{
	private int _fileCount = 0;
	private int _directoryCount = 0;
	private readonly object _lock = new();

	private readonly SynchronizationContext _syncContext;

	public event EventHandler<int>? FileCountChanged;
	public event EventHandler<int>? DirectoryCountChanged;
	public event EventHandler<FileAndDirectoryCount>? CombinedCountChanged;

	public TraversalProgressCounter()
	{
		_syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
	}

	public void IncrementFileCount(int incrementWith = 1)
	{
		if (incrementWith == 0) return;
		FileAndDirectoryCount currentCount;
		lock (_lock)
		{
			_fileCount += incrementWith;
			currentCount = new (_fileCount, _directoryCount);
		}
		PostEvent(FileCountChanged, currentCount.FileCount);
		PostEvent(CombinedCountChanged, currentCount);
	}

	public void IncrementDirectoryCount(int incrementWith = 1)
	{
		if (incrementWith == 0) return;
		FileAndDirectoryCount currentCount;
		lock (_lock)
		{
			_directoryCount += incrementWith;
			currentCount = new (_fileCount, _directoryCount);
		}
		PostEvent(DirectoryCountChanged, currentCount.DirectoryCount);
		PostEvent(CombinedCountChanged, currentCount);
	}

	private void PostEvent<T>(EventHandler<T>? handler, T value)
	{
		if (handler != null)
		{
			_syncContext.Post(_ => handler(this, value), null);
		}
	}
}
public record FileAndDirectoryCount(int FileCount, int DirectoryCount);