using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.StateMachine;
using Xunit;

namespace BMTP3.Core2.Tests.StateMachine
{
    /// <summary>
    /// Minimal IContent implementation that carries no real data, used only to satisfy
    /// BackupItem.Create's non-null content requirement.
    /// </summary>
    internal sealed class StubContent : IContent
    {
        public ulong Length => 0;
        public Stream OpenRead() => Stream.Null;
        public Task<Stream> OpenReadStreamAsync(CancellationToken ct) => Task.FromResult(Stream.Null);
        public void Dispose() { }
    }

    public class ItemStateMachineTests
    {
        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static BackupItem NewItem() =>
            BackupItem.Create(new StubContent(), "test.txt");

        private static ItemStateMachine Sut() => new ItemStateMachine();

        // ---------------------------------------------------------------
        // CanTransitionLifecycle – valid paths
        // ---------------------------------------------------------------

        [Fact]
        public void CanTransitionLifecycle_NewToQueued_ReturnsTrue()
        {
            var sut = Sut();
            Assert.True(sut.CanTransitionLifecycle(ItemLifecycleState.New, ItemLifecycleState.Queued));
        }

        [Fact]
        public void CanTransitionLifecycle_QueuedToActive_ReturnsTrue()
        {
            var sut = Sut();
            Assert.True(sut.CanTransitionLifecycle(ItemLifecycleState.Queued, ItemLifecycleState.Active));
        }

        [Fact]
        public void CanTransitionLifecycle_ActiveToProcessed_ReturnsTrue()
        {
            var sut = Sut();
            Assert.True(sut.CanTransitionLifecycle(ItemLifecycleState.Active, ItemLifecycleState.Processed));
        }

        // ---------------------------------------------------------------
        // CanTransitionLifecycle – invalid paths
        // ---------------------------------------------------------------

        [Fact]
        public void CanTransitionLifecycle_NewToActive_ReturnsFalse()
        {
            var sut = Sut();
            Assert.False(sut.CanTransitionLifecycle(ItemLifecycleState.New, ItemLifecycleState.Active));
        }

        [Fact]
        public void CanTransitionLifecycle_ProcessedToAny_ReturnsFalse()
        {
            var sut = Sut();
            Assert.False(sut.CanTransitionLifecycle(ItemLifecycleState.Processed, ItemLifecycleState.Active));
            Assert.False(sut.CanTransitionLifecycle(ItemLifecycleState.Processed, ItemLifecycleState.Queued));
            Assert.False(sut.CanTransitionLifecycle(ItemLifecycleState.Processed, ItemLifecycleState.New));
        }

        // ---------------------------------------------------------------
        // Queue / Activate – happy-path mutations on BackupItem
        // ---------------------------------------------------------------

        [Fact]
        public void Queue_FromNewItem_SetsLifecycleToQueued()
        {
            var item = NewItem();
            var sut = Sut();

            sut.Queue(item);

            Assert.Equal(ItemLifecycleState.Queued, item.LifecycleState);
            Assert.Equal(ItemResultState.Pending, item.ResultState);
        }

        [Fact]
        public void Activate_FromQueuedItem_SetsLifecycleToActive()
        {
            var item = NewItem();
            var sut = Sut();

            sut.Queue(item);
            sut.Activate(item);

            Assert.Equal(ItemLifecycleState.Active, item.LifecycleState);
        }

        // ---------------------------------------------------------------
        // Queue / Activate – invalid transition throws
        // ---------------------------------------------------------------

        [Fact]
        public void Queue_FromAlreadyQueuedItem_ThrowsInvalidOperationException()
        {
            var item = NewItem();
            var sut = Sut();
            sut.Queue(item); // New -> Queued

            Assert.Throws<InvalidOperationException>(() => sut.Queue(item));
        }

        [Fact]
        public void Activate_FromNewItem_ThrowsInvalidOperationException()
        {
            var item = NewItem();
            var sut = Sut();

            // Item is still New, skipping Queue should fail
            Assert.Throws<InvalidOperationException>(() => sut.Activate(item));
        }

        // ---------------------------------------------------------------
        // ApplyResult – each valid ItemResultState
        // ---------------------------------------------------------------

        [Fact]
        public void ApplyResult_Success_SetsResultStateAndProcessedLifecycle()
        {
            var item = NewItem();
            var sut = Sut();
            sut.Queue(item);
            sut.Activate(item);

            sut.ApplyResult(item, ItemResultState.Success);

            Assert.Equal(ItemResultState.Success, item.ResultState);
            Assert.Equal(ItemLifecycleState.Processed, item.LifecycleState);
        }

        [Fact]
        public void ApplyResult_Failed_SetsResultStateAndAddsError()
        {
            var item = NewItem();
            var sut = Sut();
            sut.Queue(item);
            sut.Activate(item);

            sut.ApplyResult(item, ItemResultState.Failed, "disk full");

            Assert.Equal(ItemResultState.Failed, item.ResultState);
            Assert.Equal(ItemLifecycleState.Processed, item.LifecycleState);
            Assert.True(item.Errors.HasErrors);
        }

        [Fact]
        public void ApplyResult_Skipped_SetsResultStateToSkipped()
        {
            var item = NewItem();
            var sut = Sut();
            sut.Queue(item);
            sut.Activate(item);

            sut.ApplyResult(item, ItemResultState.Skipped);

            Assert.Equal(ItemResultState.Skipped, item.ResultState);
            Assert.Equal(ItemLifecycleState.Processed, item.LifecycleState);
        }

        [Fact]
        public void ApplyResult_FromAlreadySucceeded_ThrowsInvalidOperationException()
        {
            var item = NewItem();
            var sut = Sut();
            sut.Queue(item);
            sut.Activate(item);
            sut.ApplyResult(item, ItemResultState.Success);

            // Once in a terminal result state, no further result transition is allowed
            Assert.Throws<InvalidOperationException>(() => sut.ApplyResult(item, ItemResultState.Failed));
        }
    }
}
