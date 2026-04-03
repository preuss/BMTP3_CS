using BMTP3.Core2.BackupNew.Engine.Models;
using BMTP3.Core2.BackupNew.Engine.Resilience;
using BMTP3.Core2.BackupNew.Engine.Transfers;

namespace BMTP3.Core2.Tests.Transfers;

public class LocalFileTransferTests
{
    /// <summary>
    /// Simple no-retry policy that executes the action exactly once.
    /// </summary>
    private sealed class NoopRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return action();
        }
    }

    private static LocalFileTransfer CreateTransfer() =>
        new LocalFileTransfer(new NoopRetryPolicy());

    [Fact]
    public async Task TransferAsync_CopiesSourceToDestination()
    {
        string srcFile = Path.GetTempFileName();
        string destFile = Path.Combine(Path.GetTempPath(), $"bmtp3_dest_{Guid.NewGuid():N}.tmp");

        try
        {
            byte[] content = System.Text.Encoding.UTF8.GetBytes("transfer test content");
            await File.WriteAllBytesAsync(srcFile, content);

            var transfer = CreateTransfer();
            OperationResult result = await transfer.TransferAsync(srcFile, destFile, dryRun: false, CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(File.Exists(destFile));
            byte[] written = await File.ReadAllBytesAsync(destFile);
            Assert.Equal(content, written);
            // Source should have been deleted after successful copy
            Assert.False(File.Exists(srcFile));
        }
        finally
        {
            if(File.Exists(srcFile)) File.Delete(srcFile);
            if(File.Exists(destFile)) File.Delete(destFile);
        }
    }

    [Fact]
    public async Task TransferAsync_DryRun_DoesNotCreateDestinationFile()
    {
        string srcFile = Path.GetTempFileName();
        string destFile = Path.Combine(Path.GetTempPath(), $"bmtp3_dryrun_{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(srcFile, "should not be copied");

            var transfer = CreateTransfer();
            OperationResult result = await transfer.TransferAsync(srcFile, destFile, dryRun: true, CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(File.Exists(destFile), "Dry run must not create the destination file.");
            // Source must remain untouched
            Assert.True(File.Exists(srcFile));
        }
        finally
        {
            if(File.Exists(srcFile)) File.Delete(srcFile);
            if(File.Exists(destFile)) File.Delete(destFile);
        }
    }

    [Fact]
    public async Task TransferAsync_MissingSourceFile_ReturnsFail()
    {
        string missingSource = Path.Combine(Path.GetTempPath(), $"bmtp3_nosuchfile_{Guid.NewGuid():N}.tmp");
        string destFile = Path.Combine(Path.GetTempPath(), $"bmtp3_dest_{Guid.NewGuid():N}.tmp");

        try
        {
            var transfer = CreateTransfer();
            OperationResult result = await transfer.TransferAsync(missingSource, destFile, dryRun: false, CancellationToken.None);

            Assert.False(result.Success);
            Assert.False(File.Exists(destFile));
        }
        finally
        {
            if(File.Exists(destFile)) File.Delete(destFile);
        }
    }

    [Fact]
    public async Task TransferAsync_CancellationToken_RespectsCancel()
    {
        string srcFile = Path.GetTempFileName();
        string destFile = Path.Combine(Path.GetTempPath(), $"bmtp3_cancel_{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(srcFile, "cancel test content");

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // already cancelled before we start

            var transfer = CreateTransfer();

            // Either an OperationCanceledException is thrown or the result is a failure.
            // Both are acceptable; what is NOT acceptable is returning Success.
            bool threwCancelled = false;
            OperationResult? result = null;

            try
            {
                result = await transfer.TransferAsync(srcFile, destFile, dryRun: false, cts.Token);
            }
            catch(OperationCanceledException)
            {
                threwCancelled = true;
            }

            Assert.True(threwCancelled || (result != null && !result.Success),
                "Expected either an OperationCanceledException or a Fail result when cancellation is pre-requested.");
        }
        finally
        {
            if(File.Exists(srcFile)) File.Delete(srcFile);
            if(File.Exists(destFile)) File.Delete(destFile);
        }
    }
}
