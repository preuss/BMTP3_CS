using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;

namespace BMTP3.Core2.Tests.Internal;

public class ProgressTrackerTests
{
	// ---------------------------------------------------------------
	// Helper
	// ---------------------------------------------------------------

	private static ProgressTracker NewTracker()
	{
		return new ProgressTracker();
	}

	// ---------------------------------------------------------------
	// SetPhase
	// ---------------------------------------------------------------

	[Fact]
	public void SetPhase_Traversing_SnapshotReflectsNewPhase()
	{
		ProgressTracker tracker = NewTracker();

		tracker.SetPhase(BackupPhase.Traversing);
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(BackupPhase.Traversing, snapshot.Phase);
	}

	[Fact]
	public void SetPhase_Completed_SnapshotReflectsCompletedPhase()
	{
		ProgressTracker tracker = NewTracker();

		tracker.SetPhase(BackupPhase.Completed);
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(BackupPhase.Completed, snapshot.Phase);
	}

	// ---------------------------------------------------------------
	// AddDiscovery
	// ---------------------------------------------------------------

	[Fact]
	public void AddDiscovery_IsFile_IncrementsFilesDiscoveredAndBytesTotal()
	{
		ProgressTracker tracker = NewTracker();

		tracker.AddDiscovery(false, 1024);
		tracker.AddDiscovery(false, 512);
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(2, snapshot.FilesDiscovered);
		Assert.Equal(1536, snapshot.BytesTotal);
	}

	[Fact]
	public void AddDiscovery_IsDirectory_IncrementsDirectoriesTraversedOnly()
	{
		ProgressTracker tracker = NewTracker();

		tracker.AddDiscovery(true);
		tracker.AddDiscovery(true);
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(2, snapshot.DirectoriesTraversed);
		Assert.Equal(0, snapshot.FilesDiscovered);
		Assert.Equal(0, snapshot.BytesTotal);
	}

	// ---------------------------------------------------------------
	// CompleteItem
	// ---------------------------------------------------------------

	[Fact]
	public void CompleteItem_Success_IncrementsFilesSucceeded()
	{
		ProgressTracker tracker = NewTracker();

		tracker.CompleteItem("item-1", ItemResultState.Success, 100);
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(1, snapshot.FilesSucceeded);
		Assert.Equal(0, snapshot.FilesSkipped);
		Assert.Equal(0, snapshot.FilesFailed);
	}

	[Fact]
	public void CompleteItem_Skipped_IncrementsFilesSkipped()
	{
		ProgressTracker tracker = NewTracker();

		tracker.CompleteItem("item-2", ItemResultState.Skipped, 200);
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(0, snapshot.FilesSucceeded);
		Assert.Equal(1, snapshot.FilesSkipped);
		Assert.Equal(0, snapshot.FilesFailed);
	}

	[Fact]
	public void CompleteItem_Failed_IncrementsFilesFailed()
	{
		ProgressTracker tracker = NewTracker();

		tracker.CompleteItem("item-3", ItemResultState.Failed, 300);
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(0, snapshot.FilesSucceeded);
		Assert.Equal(0, snapshot.FilesSkipped);
		Assert.Equal(1, snapshot.FilesFailed);
	}

	[Fact]
	public void CompleteItem_MixedResults_AllCountersAreAccurate()
	{
		ProgressTracker tracker = NewTracker();

		tracker.CompleteItem("a", ItemResultState.Success, 10);
		tracker.CompleteItem("b", ItemResultState.Success, 10);
		tracker.CompleteItem("c", ItemResultState.Skipped, 5);
		tracker.CompleteItem("d", ItemResultState.Failed, 20);
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(2, snapshot.FilesSucceeded);
		Assert.Equal(1, snapshot.FilesSkipped);
		Assert.Equal(1, snapshot.FilesFailed);
		Assert.Equal(4, snapshot.FilesProcessed);
	}

	// ---------------------------------------------------------------
	// Concurrent AddDiscovery
	// ---------------------------------------------------------------

	[Fact]
	public void AddDiscovery_ConcurrentFileCalls_DoNotCorruptTotals()
	{
		const int iterations = 100;
		const long sizePerFile = 512;
		ProgressTracker tracker = NewTracker();

		Parallel.For(0, iterations, _ => { tracker.AddDiscovery(false, sizePerFile); });

		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(iterations, snapshot.FilesDiscovered);
		Assert.Equal(iterations * sizePerFile, snapshot.BytesTotal);
	}

	// ---------------------------------------------------------------
	// GetSnapshot – initial state
	// ---------------------------------------------------------------

	[Fact]
	public void GetSnapshot_InitialState_AllCountersAreZeroAndPhaseIsStarting()
	{
		ProgressTracker tracker = NewTracker();
		BackupProgress snapshot = tracker.GetSnapshot();

		Assert.Equal(BackupPhase.Starting, snapshot.Phase);
		Assert.Equal(0, snapshot.FilesDiscovered);
		Assert.Equal(0, snapshot.BytesTotal);
		Assert.Equal(0, snapshot.FilesSucceeded);
		Assert.Equal(0, snapshot.FilesSkipped);
		Assert.Equal(0, snapshot.FilesFailed);
		Assert.Equal(0, snapshot.FilesProcessed);
		Assert.Empty(snapshot.ActiveFiles);
	}
}