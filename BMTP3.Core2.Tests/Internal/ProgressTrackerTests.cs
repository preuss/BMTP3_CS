using System;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Internal;
using Xunit;

namespace BMTP3.Core2.Tests.Internal
{
    public class ProgressTrackerTests
    {
        // ---------------------------------------------------------------
        // Helper
        // ---------------------------------------------------------------

        private static ProgressTracker NewTracker() => new ProgressTracker();

        // ---------------------------------------------------------------
        // SetPhase
        // ---------------------------------------------------------------

        [Fact]
        public void SetPhase_Traversing_SnapshotReflectsNewPhase()
        {
            var tracker = NewTracker();

            tracker.SetPhase(BackupPhase.Traversing);
            var snapshot = tracker.GetSnapshot();

            Assert.Equal(BackupPhase.Traversing, snapshot.Phase);
        }

        [Fact]
        public void SetPhase_Completed_SnapshotReflectsCompletedPhase()
        {
            var tracker = NewTracker();

            tracker.SetPhase(BackupPhase.Completed);
            var snapshot = tracker.GetSnapshot();

            Assert.Equal(BackupPhase.Completed, snapshot.Phase);
        }

        // ---------------------------------------------------------------
        // AddDiscovery
        // ---------------------------------------------------------------

        [Fact]
        public void AddDiscovery_IsFile_IncrementsFilesDiscoveredAndBytesTotal()
        {
            var tracker = NewTracker();

            tracker.AddDiscovery(isDirectory: false, size: 1024);
            tracker.AddDiscovery(isDirectory: false, size: 512);
            var snapshot = tracker.GetSnapshot();

            Assert.Equal(2, snapshot.FilesDiscovered);
            Assert.Equal(1536, snapshot.BytesTotal);
        }

        [Fact]
        public void AddDiscovery_IsDirectory_IncrementsDirectoriesTraversedOnly()
        {
            var tracker = NewTracker();

            tracker.AddDiscovery(isDirectory: true);
            tracker.AddDiscovery(isDirectory: true);
            var snapshot = tracker.GetSnapshot();

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
            var tracker = NewTracker();

            tracker.CompleteItem("item-1", ItemResultState.Success, totalBytes: 100);
            var snapshot = tracker.GetSnapshot();

            Assert.Equal(1, snapshot.FilesSucceeded);
            Assert.Equal(0, snapshot.FilesSkipped);
            Assert.Equal(0, snapshot.FilesFailed);
        }

        [Fact]
        public void CompleteItem_Skipped_IncrementsFilesSkipped()
        {
            var tracker = NewTracker();

            tracker.CompleteItem("item-2", ItemResultState.Skipped, totalBytes: 200);
            var snapshot = tracker.GetSnapshot();

            Assert.Equal(0, snapshot.FilesSucceeded);
            Assert.Equal(1, snapshot.FilesSkipped);
            Assert.Equal(0, snapshot.FilesFailed);
        }

        [Fact]
        public void CompleteItem_Failed_IncrementsFilesFailed()
        {
            var tracker = NewTracker();

            tracker.CompleteItem("item-3", ItemResultState.Failed, totalBytes: 300);
            var snapshot = tracker.GetSnapshot();

            Assert.Equal(0, snapshot.FilesSucceeded);
            Assert.Equal(0, snapshot.FilesSkipped);
            Assert.Equal(1, snapshot.FilesFailed);
        }

        [Fact]
        public void CompleteItem_MixedResults_AllCountersAreAccurate()
        {
            var tracker = NewTracker();

            tracker.CompleteItem("a", ItemResultState.Success, totalBytes: 10);
            tracker.CompleteItem("b", ItemResultState.Success, totalBytes: 10);
            tracker.CompleteItem("c", ItemResultState.Skipped, totalBytes: 5);
            tracker.CompleteItem("d", ItemResultState.Failed, totalBytes: 20);
            var snapshot = tracker.GetSnapshot();

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
            var tracker = NewTracker();

            Parallel.For(0, iterations, _ =>
            {
                tracker.AddDiscovery(isDirectory: false, size: sizePerFile);
            });

            var snapshot = tracker.GetSnapshot();

            Assert.Equal(iterations, snapshot.FilesDiscovered);
            Assert.Equal(iterations * sizePerFile, snapshot.BytesTotal);
        }

        // ---------------------------------------------------------------
        // GetSnapshot – initial state
        // ---------------------------------------------------------------

        [Fact]
        public void GetSnapshot_InitialState_AllCountersAreZeroAndPhaseIsStarting()
        {
            var tracker = NewTracker();
            var snapshot = tracker.GetSnapshot();

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
}
