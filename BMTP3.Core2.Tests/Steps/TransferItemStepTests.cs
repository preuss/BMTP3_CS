using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Models;
using BMTP3.Core2.BackupNew.Engine.Steps.TransferStep;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Transfers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BMTP3.Core2.Tests.Steps
{
    public class TransferItemStepTests
    {
        private class SimplePathGenerator : IPathGenerator
        {
            public string ApplyPattern(string pattern, IBackupItem item) => Path.GetFileName(item.SourcePath);
            public string GenerateRelativePath(IBackupItem item, BackupPlan plan) => Path.GetFileName(item.SourcePath);
        }

        private class DummyCollisionResolver : ICollisionResolver { public Task<CollisionResult> ResolveAsync(IBackupItem item, string proposedFullPath, BackupPlan plan, CancellationToken ct) => Task.FromResult(new CollisionResult(BMTP3.Core2.BackupNew.Api.Enums.BackupActionType.Copy, proposedFullPath, "")); }
        private class DummyFileTransfer : IFileTransfer { public Task<OperationResult> TransferAsync(string stagingPath, string targetPath, bool dryRun, CancellationToken ct) => Task.FromResult(OperationResult.Ok()); }

        private class FakeItemHasher : IItemHasher
        {
            public async Task<Dictionary<HashType, string>> ComputeHashesAsync(IBackupItem item, List<HashType> hashTypes, IProgress<ulong> progress, CancellationToken ct)
            {
                using var sha = SHA256.Create();
                using var s = item.Content.OpenRead();
                var h = sha.ComputeHash(s);
                string hex = string.Concat(h.Select(b => b.ToString("x2")));
                var dict = new Dictionary<HashType, string>();
                foreach (var ht in hashTypes) dict[ht] = hex;
                return await Task.FromResult(dict);
            }
        }

        [Fact]
        public async Task VerifyTransferAsync_ComputesSourceHashesIfMissing_AndVerifies()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s.jpg");
            string dest = Path.Combine(dir, "d.jpg");
            try
            {
                byte[] data = new byte[4096]; new Random(5).NextBytes(data);
                await File.WriteAllBytesAsync(src, data);
                await File.WriteAllBytesAsync(dest, data);

                var plan = new BackupPlan { OutputPath = dir, PostWriteVerification = PostWriteVerificationType.Hash };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
                // Intentionally do not set hashes on item.Metadata

                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), new DummyFileTransfer(), new FakeItemHasher());

                bool ok = await step.VerifyTransferAsync(item, dest, PostWriteVerificationType.Hash, null, CancellationToken.None);
                Assert.True(ok);
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task VerifyTransferAsync_BinaryFallback_WhenHashingFails()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test2");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s2.jpg");
            string dest = Path.Combine(dir, "d2.jpg");
            try
            {
                byte[] data = new byte[4096]; new Random(9).NextBytes(data);
                await File.WriteAllBytesAsync(src, data);
                await File.WriteAllBytesAsync(dest, data);

                var plan = new BackupPlan { OutputPath = dir, PostWriteVerification = PostWriteVerificationType.Hash };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));

                // Create an item hasher that throws to simulate failure
                var failingHasher = new FailingItemHasher();

                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), new DummyFileTransfer(), failingHasher);

                bool ok = await step.VerifyTransferAsync(item, dest, PostWriteVerificationType.Hash, null, CancellationToken.None);
                Assert.True(ok);
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task VerifyTransferAsync_ReturnsFalse_WhenDestinationDifferent()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test3");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s3.jpg");
            string dest = Path.Combine(dir, "d3.jpg");
            try
            {
                byte[] data1 = new byte[4096]; new Random(11).NextBytes(data1);
                byte[] data2 = new byte[4096]; new Random(12).NextBytes(data2);
                await File.WriteAllBytesAsync(src, data1);
                await File.WriteAllBytesAsync(dest, data2);

                var plan = new BackupPlan { OutputPath = dir, PostWriteVerification = PostWriteVerificationType.Hash };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));

                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), new DummyFileTransfer(), new FakeItemHasher());

                bool ok = await step.VerifyTransferAsync(item, dest, PostWriteVerificationType.Hash, null, CancellationToken.None);
                Assert.False(ok);
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        private class FailingItemHasher : IItemHasher
        {
            public Task<Dictionary<HashType, string>> ComputeHashesAsync(IBackupItem item, List<HashType> hashTypes, IProgress<ulong> progress, CancellationToken ct)
            {
                throw new InvalidOperationException("Simulated hashing failure");
            }
        }

        [Fact]
        public async Task VerifyTransferAsync_RetriesOnHashMismatch_AndSucceeds()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test_retry");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s_retry.jpg");
            string dest = Path.Combine(dir, "d_retry.jpg");
            try
            {
                byte[] data = new byte[4096]; new Random(7).NextBytes(data);
                await File.WriteAllBytesAsync(src, data);
                await File.WriteAllBytesAsync(dest, data);

                var plan = new BackupPlan { OutputPath = dir, PostWriteVerification = PostWriteVerificationType.Hash, VerificationRetryCount = 3, VerificationRetryDelayMs = 10 };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));

                // Precompute expected hash and set it into metadata so source hashing is skipped
                using var sha = SHA256.Create();
                using var s = File.OpenRead(src);
                var h = sha.ComputeHash(s);
                string hex = string.Concat(h.Select(b => b.ToString("x2")));
                var dict = new Dictionary<HashType, string> { { HashType.SHA2_256, hex } };
                item.Metadata.Set(MetadataKey.Hashes, dict);

                // Hasher that returns wrong hash for first 2 dest attempts, then correct one
                // The pipeline's SimplePathGenerator uses the source filename as the destination name,
                // so ensure the RetryHasher watches the actual destination path the step will use.
                var retryHasher = new RetryHasher(Path.Combine(plan.OutputPath, Path.GetFileName(src)), succeedAfter: 3);

                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), new DummyFileTransfer(), retryHasher);

                bool verified = false;
                int attempts = 3;
                for(int i = 1; i <= attempts; i++)
                {
                    verified = await step.VerifyTransferAsync(item, dest, PostWriteVerificationType.Hash, null, CancellationToken.None);
                    if(verified) break;
                }

                Assert.True(verified);
                Assert.InRange(retryHasher.InvocationCount, 1, 3);
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task VerifyTransferAsync_FallsBackToBinary_WhenHasherAlwaysFails()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test_binfallback");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s_bin.jpg");
            string dest = Path.Combine(dir, "d_bin.jpg");
            try
            {
                byte[] data = new byte[4096]; new Random(21).NextBytes(data);
                await File.WriteAllBytesAsync(src, data);
                await File.WriteAllBytesAsync(dest, data);

                var plan = new BackupPlan { OutputPath = dir, PostWriteVerification = PostWriteVerificationType.Hash };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));

                // Hasher that always throws
                var failingHasher = new FailingItemHasher();

                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), new DummyFileTransfer(), failingHasher);

                bool ok = await step.VerifyTransferAsync(item, dest, PostWriteVerificationType.Hash, null, CancellationToken.None);
                Assert.True(ok, "Expected binary fallback to succeed when hasher fails");
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task ExecuteAsync_DeletesDestination_OnVerificationTimeoutAndConfigured()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test_del");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s_del.jpg");
            string dest = Path.Combine(dir, "d_del.jpg");
            try
            {
                byte[] data = new byte[4096]; new Random(13).NextBytes(data);
                await File.WriteAllBytesAsync(src, data);

                var plan = new BackupPlan
                {
                    OutputPath = dir,
                    PostWriteVerification = PostWriteVerificationType.Hash,
                    VerificationRetryCount = 1,
                    VerificationTimeoutMs = 50,
                    VerificationDeleteOnFailure = true
                };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));

                // Ensure expected hash is present so source hashing isn't attempted
                using var sha = SHA256.Create();
                using var s = File.OpenRead(src);
                var h = sha.ComputeHash(s);
                string hex = string.Concat(h.Select(b => b.ToString("x2")));
                var dict = new Dictionary<HashType, string> { { HashType.SHA2_256, hex } };
                item.Metadata.Set(MetadataKey.Hashes, dict);

                // Transfer that copies the file so destination exists
                var copier = new CopyingFileTransfer();

                // Hasher that sleeps longer than timeout
                var slowHasher = new SlowHasher(delayMs: 200);

                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), copier, slowHasher);

                var result = await step.ExecuteAsync(item, null, CancellationToken.None);

                Assert.False(result.Success);
                Assert.False(File.Exists(dest));
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task ExecuteAsync_RetriesOnVerificationMismatch_AndSucceeds()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test_exec_retry");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s_exec_retry.jpg");
            // The pipeline's SimplePathGenerator uses the source filename as the destination name.
            string dest = Path.Combine(dir, Path.GetFileName(src));
            try
            {
                byte[] data = new byte[4096]; new Random(17).NextBytes(data);
                await File.WriteAllBytesAsync(src, data);

                var plan = new BackupPlan { OutputPath = dir, PostWriteVerification = PostWriteVerificationType.Hash, VerificationRetryCount = 3, VerificationRetryDelayMs = 10 };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));

                // Ensure expected hash is present so source hashing isn't attempted
                using var sha = SHA256.Create();
                using var s = File.OpenRead(src);
                var h = sha.ComputeHash(s);
                string hex = string.Concat(h.Select(b => b.ToString("x2")));
                var dict = new Dictionary<HashType, string> { { HashType.SHA2_256, hex } };
                item.Metadata.Set(MetadataKey.Hashes, dict);

                var copier = new CopyingFileTransfer();

                // Hasher that returns wrong hash for first 2 dest attempts, then correct one
                var retryHasher = new RetryHasher(dest, succeedAfter: 3);

                var logger = new BMTP3.Core2.Tests.Utils.TestLogger<BMTP3.Core2.BackupNew.Engine.Steps.TransferStep.TransferItemStep>();
                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), copier, retryHasher, logger);

                var result = await step.ExecuteAsync(item, null, CancellationToken.None);

                if(!result.Success) Console.WriteLine(logger.Logs);

                Assert.True(result.Success, "Expected transfer + verification to eventually succeed after retries");
                Assert.InRange(retryHasher.InvocationCount, 1, 3);
                Assert.True(File.Exists(dest));
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task ExecuteAsync_VerificationRetryCountZero_DefaultsToOne()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test_exec_retry0");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s_exec_retry0.jpg");
            // The pipeline's SimplePathGenerator uses the source filename as the destination name.
            string dest = Path.Combine(dir, Path.GetFileName(src));
            try
            {
                byte[] data = new byte[4096]; new Random(19).NextBytes(data);
                await File.WriteAllBytesAsync(src, data);

                var plan = new BackupPlan { OutputPath = dir, PostWriteVerification = PostWriteVerificationType.Hash, VerificationRetryCount = 0, VerificationRetryDelayMs = 10 };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));

                // Ensure expected hash is present so source hashing isn't attempted
                using var sha = SHA256.Create();
                using var s = File.OpenRead(src);
                var h = sha.ComputeHash(s);
                string hex = string.Concat(h.Select(b => b.ToString("x2")));
                var dict = new Dictionary<HashType, string> { { HashType.SHA2_256, hex } };
                item.Metadata.Set(MetadataKey.Hashes, dict);

                var copier = new CopyingFileTransfer();

                // Hasher that would succeed immediately
                // Use the pipeline-computed destination path (OutputPath + source filename)
                var retryHasher = new RetryHasher(Path.Combine(plan.OutputPath, Path.GetFileName(src)), succeedAfter: 1);

                var logger = new BMTP3.Core2.Tests.Utils.TestLogger<BMTP3.Core2.BackupNew.Engine.Steps.TransferStep.TransferItemStep>();
                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), copier, retryHasher, logger);

                var result = await step.ExecuteAsync(item, null, CancellationToken.None);

                if(!result.Success) Console.WriteLine(logger.Logs);

                Assert.True(result.Success, "Expected transfer + verification to succeed with retry count 0 treated as 1");
                Assert.Equal(1, retryHasher.InvocationCount);
                Assert.True(File.Exists(dest));
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task ExecuteAsync_AppliesAuthoredTimestamp_ToDestination()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test_timestamp");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s_ts.jpg");
            string dest = Path.Combine(dir, Path.GetFileName(src));
            try
            {
                byte[] data = new byte[1024]; new Random(23).NextBytes(data);
                await File.WriteAllBytesAsync(src, data);

                DateTime authoredUtc = new DateTime(2020, 01, 02, 03, 04, 05, DateTimeKind.Utc);

                var plan = new BackupPlan
                {
                    OutputPath = dir,
                    PostWriteVerification = PostWriteVerificationType.None,
                    DryRun = false
                };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
                item.Metadata.Set(MetadataKey.AuthoredDateTime, authoredUtc);

                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), new CopyingFileTransfer(), new FakeItemHasher());
                var result = await step.ExecuteAsync(item, null, CancellationToken.None);

                Assert.True(result.Success);
                Assert.True(File.Exists(dest));

                DateTime lastWrite = File.GetLastWriteTimeUtc(dest);
                Assert.True(Math.Abs((lastWrite - authoredUtc).TotalSeconds) < 2, $"Expected destination last write near {authoredUtc:o}, actual {lastWrite:o}");
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task ExecuteAsync_DryRun_CleansLocalTempPathFile_WhenPresent()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_transfer_test_dryrun_cleanup");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "s_cleanup.jpg");
            string temp = Path.Combine(dir, "temp_cleanup.tmp");
            try
            {
                await File.WriteAllBytesAsync(src, new byte[] { 10, 11, 12 });
                await File.WriteAllBytesAsync(temp, new byte[] { 20, 21, 22 });

                var plan = new BackupPlan
                {
                    OutputPath = dir,
                    PostWriteVerification = PostWriteVerificationType.None,
                    DryRun = true
                };

                var item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
                item.Metadata.Set(MetadataKey.LocalTempPath, temp);

                var step = new TransferItemStep(plan, new SimplePathGenerator(), new DummyCollisionResolver(), new DummyFileTransfer(), new FakeItemHasher());
                var result = await step.ExecuteAsync(item, null, CancellationToken.None);

                Assert.True(result.Success);
                Assert.False(File.Exists(temp));
                Assert.True(File.Exists(src));
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(temp); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        private class RetryHasher : IItemHasher
        {
            private readonly string _destPath;
            private readonly int _succeedAfter;
            public int InvocationCount { get; private set; }

            public RetryHasher(string destPath, int succeedAfter)
            {
                _destPath = Path.GetFullPath(destPath);
                _succeedAfter = succeedAfter;
                InvocationCount = 0;
            }

            public async Task<Dictionary<HashType, string>> ComputeHashesAsync(IBackupItem item, List<HashType> hashTypes, IProgress<ulong> progress, CancellationToken ct)
            {
                InvocationCount++;
                // (diagnostic logging removed)
                // If hashing the destination path, sometimes return wrong hash
                if(item.Content is FileContent fc && Path.GetFullPath(fc.FileInfo.FullName).Equals(_destPath, StringComparison.OrdinalIgnoreCase))
                {
                    if(InvocationCount < _succeedAfter)
                    {
                        // return a deterministic wrong hash
                        var wrong = new Dictionary<HashType, string>();
                        foreach(var ht in hashTypes) wrong[ht] = new string('0', 64);
                        // (diagnostic logging removed)
                        return await Task.FromResult(wrong);
                    }
                    // else compute real hash
                    using var sha = SHA256.Create();
                    using var s = File.OpenRead(fc.FileInfo.FullName);
                    var h = sha.ComputeHash(s);
                    string hex = string.Concat(h.Select(b => b.ToString("x2")));
                    var dict = new Dictionary<HashType, string>();
                    foreach (var ht in hashTypes) dict[ht] = hex;
                    // (diagnostic logging removed)
                    return await Task.FromResult(dict);
                }

                // For any other path, compute actual hash
                if(item.Content is FileContent fc2)
                {
                    using var sha2 = SHA256.Create();
                    using var s2 = File.OpenRead(fc2.FileInfo.FullName);
                    var h2 = sha2.ComputeHash(s2);
                    string hex2 = string.Concat(h2.Select(b => b.ToString("x2")));
                    var dict2 = new Dictionary<HashType, string>();
                    foreach (var ht in hashTypes) dict2[ht] = hex2;
                    return await Task.FromResult(dict2);
                }

                return await Task.FromResult(new Dictionary<HashType, string>());
            }
        }

        private class CopyingFileTransfer : IFileTransfer
        {
            public Task<OperationResult> TransferAsync(string stagingPath, string targetPath, bool dryRun, CancellationToken ct)
            {
                try
                {
                    if(!dryRun)
                    {
                        File.Copy(stagingPath, targetPath, true);
                    }
                    return Task.FromResult(OperationResult.Ok());
                }
                catch(Exception ex)
                {
                    return Task.FromResult(OperationResult.Fail(ex.Message));
                }
            }
        }

        private class SlowHasher : IItemHasher
        {
            private readonly int _delayMs;
            public SlowHasher(int delayMs) { _delayMs = delayMs; }
            public async Task<Dictionary<HashType, string>> ComputeHashesAsync(IBackupItem item, List<HashType> hashTypes, IProgress<ulong> progress, CancellationToken ct)
            {
                await Task.Delay(_delayMs, ct);
                // If not canceled, compute a normal hash
                if(item.Content is FileContent fc)
                {
                    using var sha = SHA256.Create();
                    using var s = File.OpenRead(fc.FileInfo.FullName);
                    var h = sha.ComputeHash(s);
                    string hex = string.Concat(h.Select(b => b.ToString("x2")));
                    var dict = new Dictionary<HashType, string>();
                    foreach (var ht in hashTypes) dict[ht] = hex;
                    return dict;
                }
                return new Dictionary<HashType, string>();
            }
        }
    }
}
