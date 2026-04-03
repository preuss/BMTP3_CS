using System;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Engine.StateMachine;
using Xunit;

namespace BMTP3.Core2.Tests.StateMachine
{
    public class JobStateMachineTests
    {
        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static BackupJob NewJob() => new BackupJob("test-job");
        private static JobStateMachine Sut() => new JobStateMachine();

        // ---------------------------------------------------------------
        // CanTransition – valid paths
        // ---------------------------------------------------------------

        [Fact]
        public void CanTransition_ReadyToRunning_ReturnsTrue()
        {
            Assert.True(Sut().CanTransition(JobState.Ready, JobState.Running));
        }

        [Fact]
        public void CanTransition_RunningToCompleted_ReturnsTrue()
        {
            Assert.True(Sut().CanTransition(JobState.Running, JobState.Completed));
        }

        [Fact]
        public void CanTransition_RunningToFailed_ReturnsTrue()
        {
            Assert.True(Sut().CanTransition(JobState.Running, JobState.Failed));
        }

        [Fact]
        public void CanTransition_RunningToCancelled_ReturnsTrue()
        {
            Assert.True(Sut().CanTransition(JobState.Running, JobState.Cancelled));
        }

        [Fact]
        public void CanTransition_CancelledToRunning_ReturnsTrue()
        {
            // Cancelled jobs can be resumed (re-run)
            Assert.True(Sut().CanTransition(JobState.Cancelled, JobState.Running));
        }

        // ---------------------------------------------------------------
        // CanTransition – invalid paths
        // ---------------------------------------------------------------

        [Fact]
        public void CanTransition_ReadyToCompleted_ReturnsFalse()
        {
            Assert.False(Sut().CanTransition(JobState.Ready, JobState.Completed));
        }

        [Fact]
        public void CanTransition_CompletedToAny_ReturnsFalse()
        {
            var sut = Sut();
            Assert.False(sut.CanTransition(JobState.Completed, JobState.Running));
            Assert.False(sut.CanTransition(JobState.Completed, JobState.Failed));
            Assert.False(sut.CanTransition(JobState.Completed, JobState.Cancelled));
        }

        [Fact]
        public void CanTransition_FailedToAny_ReturnsFalse()
        {
            var sut = Sut();
            Assert.False(sut.CanTransition(JobState.Failed, JobState.Running));
            Assert.False(sut.CanTransition(JobState.Failed, JobState.Completed));
        }

        // ---------------------------------------------------------------
        // Apply – happy-path mutations on BackupJob
        // ---------------------------------------------------------------

        [Fact]
        public void Apply_ReadyToRunning_SetsJobStateToRunning()
        {
            var job = NewJob();
            var sut = Sut();

            sut.Apply(job, JobState.Running);

            Assert.Equal(JobState.Running, job.State);
        }

        [Fact]
        public void Apply_RunningToCompleted_SetsJobStateToCompleted()
        {
            var job = NewJob();
            var sut = Sut();
            sut.Apply(job, JobState.Running);

            sut.Apply(job, JobState.Completed);

            Assert.Equal(JobState.Completed, job.State);
        }

        [Fact]
        public void Apply_RunningToFailed_SetsJobStateToFailed()
        {
            var job = NewJob();
            var sut = Sut();
            sut.Apply(job, JobState.Running);

            sut.Apply(job, JobState.Failed);

            Assert.Equal(JobState.Failed, job.State);
        }

        [Fact]
        public void Apply_RunningToCancelled_SetsJobStateToCancelled()
        {
            var job = NewJob();
            var sut = Sut();
            sut.Apply(job, JobState.Running);

            sut.Apply(job, JobState.Cancelled);

            Assert.Equal(JobState.Cancelled, job.State);
        }

        // ---------------------------------------------------------------
        // Apply – invalid transition throws
        // ---------------------------------------------------------------

        [Fact]
        public void Apply_InvalidTransition_ThrowsInvalidOperationException()
        {
            var job = NewJob(); // state = Ready
            var sut = Sut();

            // Jumping directly from Ready to Completed is not allowed
            Assert.Throws<InvalidOperationException>(() => sut.Apply(job, JobState.Completed));
        }

        [Fact]
        public void Apply_CompletedToRunning_ThrowsInvalidOperationException()
        {
            var job = NewJob();
            var sut = Sut();
            sut.Apply(job, JobState.Running);
            sut.Apply(job, JobState.Completed);

            // Terminal state – no further transitions
            Assert.Throws<InvalidOperationException>(() => sut.Apply(job, JobState.Running));
        }

        // ---------------------------------------------------------------
        // Audit trail
        // ---------------------------------------------------------------

        [Fact]
        public void Apply_RunningToCompleted_WritesAuditEntry()
        {
            var job = NewJob();
            var sut = Sut();
            int auditCountBefore = job.AuditTrail.Count;

            sut.Apply(job, JobState.Running);
            sut.Apply(job, JobState.Completed);

            // Each Apply call adds one audit entry
            Assert.Equal(auditCountBefore + 2, job.AuditTrail.Count);
        }

        // ---------------------------------------------------------------
        // AttemptCount
        // ---------------------------------------------------------------

        [Fact]
        public void Apply_Running_IncrementsAttemptCount()
        {
            var job = NewJob();
            var sut = Sut();
            uint before = job.AttemptCount;

            sut.Apply(job, JobState.Running);

            Assert.Equal(before + 1, job.AttemptCount);
        }
    }
}
