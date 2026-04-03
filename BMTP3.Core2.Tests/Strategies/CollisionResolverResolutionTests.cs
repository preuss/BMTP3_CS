using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class CollisionResolverResolutionTests
    {
        // ──────────────────────────────────────────────────────────
        // Shared test doubles (mirrors pattern from existing tests)
        // ──────────────────────────────────────────────────────────

        private class SimplePathGenerator : IPathGenerator
        {
            public string ApplyPattern(string pattern, IBackupItem item) =>
                Path.Combine(Path.GetDirectoryName(item.SourcePath) ?? string.Empty, Path.GetFileName(item.SourcePath));

            public string GenerateRelativePath(IBackupItem item, BackupPlan plan) =>
                Path.GetFileName(item.SourcePath);
        }

        private class DummyMetadataReader : IMetadataReader
        {
            public Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct) => Task.CompletedTask;
        }

        private static CollisionResolver BuildResolver() =>
            new CollisionResolver(
                new DummyMetadataReader(),
                new SimplePathGenerator(),
                LoggerFactory.Create(_ => { }).CreateLogger<CollisionResolver>(),
                hashGenerator: null);

        // Helper: writes two different files to disk, returns (srcPath, destPath).
        private static async Task<(string src, string dest)> CreateDifferentFilesAsync(string dirName)
        {
            string dir = Path.Combine(Path.GetTempPath(), dirName);
            Directory.CreateDirectory(dir);

            string src  = Path.Combine(dir, "source.jpg");
            string dest = Path.Combine(dir, "dest.jpg");

            await File.WriteAllBytesAsync(src,  new byte[] { 1, 2, 3 });
            await File.WriteAllBytesAsync(dest, new byte[] { 4, 5, 6 });
            return (src, dest);
        }

        private static BackupItem CreateItemFromFile(string srcPath)
        {
            var content = new FileContent(srcPath);
            var item = BackupItem.Create(content, Path.GetFileName(srcPath));
            return item;
        }

        // ──────────────────────────────────────────────────────────
        // Overwrite resolution
        // ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ResolveAsync_Overwrite_WhenFilesAreDifferent_ReturnsCopyAction()
        {
            var (src, dest) = await CreateDifferentFilesAsync("bmtp3_cr_overwrite");
            try
            {
                var item = CreateItemFromFile(src);
                var plan = new BackupPlan
                {
                    ComparisonType    = CollisionComparisonType.Binary,
                    CollisionResolution = CollisionResolutionType.Overwrite
                };

                var result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

                Assert.Equal(BackupActionType.Copy, result.Action);
            }
            finally
            {
                TryDelete(src); TryDelete(dest);
                TryDeleteDir(Path.GetDirectoryName(dest)!);
            }
        }

        [Fact]
        public async Task ResolveAsync_Overwrite_WhenFilesAreDifferent_ReasonContainsOverwrite()
        {
            var (src, dest) = await CreateDifferentFilesAsync("bmtp3_cr_overwrite2");
            try
            {
                var item = CreateItemFromFile(src);
                var plan = new BackupPlan
                {
                    ComparisonType    = CollisionComparisonType.Binary,
                    CollisionResolution = CollisionResolutionType.Overwrite
                };

                var result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

                Assert.Contains("Overwrite", result.Reason, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                TryDelete(src); TryDelete(dest);
                TryDeleteDir(Path.GetDirectoryName(dest)!);
            }
        }

        // ──────────────────────────────────────────────────────────
        // Skip resolution
        // ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ResolveAsync_Skip_WhenFilesAreDifferent_ReturnsSkipAction()
        {
            var (src, dest) = await CreateDifferentFilesAsync("bmtp3_cr_skip");
            try
            {
                var item = CreateItemFromFile(src);
                var plan = new BackupPlan
                {
                    ComparisonType    = CollisionComparisonType.Binary,
                    CollisionResolution = CollisionResolutionType.Skip
                };

                var result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

                Assert.Equal(BackupActionType.Skip, result.Action);
            }
            finally
            {
                TryDelete(src); TryDelete(dest);
                TryDeleteDir(Path.GetDirectoryName(dest)!);
            }
        }

        // ──────────────────────────────────────────────────────────
        // Error resolution
        // ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ResolveAsync_Error_WhenFilesAreDifferent_ThrowsIOException()
        {
            var (src, dest) = await CreateDifferentFilesAsync("bmtp3_cr_error");
            try
            {
                var item = CreateItemFromFile(src);
                var plan = new BackupPlan
                {
                    ComparisonType    = CollisionComparisonType.Binary,
                    CollisionResolution = CollisionResolutionType.Error
                };

                await Assert.ThrowsAsync<IOException>(() =>
                    BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None));
            }
            finally
            {
                TryDelete(src); TryDelete(dest);
                TryDeleteDir(Path.GetDirectoryName(dest)!);
            }
        }

        // ──────────────────────────────────────────────────────────
        // Timestamp rename strategy
        // ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ResolveAsync_Rename_TimestampStrategy_ProducesPathWithTimestampSuffix()
        {
            var (src, dest) = await CreateDifferentFilesAsync("bmtp3_cr_ts");
            try
            {
                var item = CreateItemFromFile(src);
                var authored = new DateTime(2024, 6, 15, 8, 30, 45, DateTimeKind.Utc);
                item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);

                var plan = new BackupPlan
                {
                    ComparisonType    = CollisionComparisonType.Binary,
                    CollisionResolution = CollisionResolutionType.Rename,
                    RenameStrategy    = RenameStrategy.Timestamp
                };

                var result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

                // Expected suffix: _20240615_083045
                Assert.Contains("_20240615_083045", result.TargetPath);
                Assert.Equal(BackupActionType.Rename, result.Action);
            }
            finally
            {
                TryDelete(src); TryDelete(dest);
                TryDeleteDir(Path.GetDirectoryName(dest)!);
            }
        }

        // ──────────────────────────────────────────────────────────
        // Increment rename strategy
        // ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ResolveAsync_Rename_IncrementStrategy_AppendsUnderscoreOne()
        {
            var (src, dest) = await CreateDifferentFilesAsync("bmtp3_cr_incr");
            try
            {
                var item = CreateItemFromFile(src);
                var plan = new BackupPlan
                {
                    ComparisonType    = CollisionComparisonType.Binary,
                    CollisionResolution = CollisionResolutionType.Rename,
                    RenameStrategy    = RenameStrategy.Increment
                };

                var result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

                // The generated path should not exist and should contain _1
                string expectedName = Path.GetFileNameWithoutExtension(dest) + "_1" + Path.GetExtension(dest);
                Assert.Equal(expectedName, Path.GetFileName(result.TargetPath));
                Assert.Equal(BackupActionType.Rename, result.Action);
            }
            finally
            {
                TryDelete(src); TryDelete(dest);
                TryDeleteDir(Path.GetDirectoryName(dest)!);
            }
        }

        [Fact]
        public async Task ResolveAsync_Rename_IncrementStrategy_ParallelCalls_ReturnUniqueTargets()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_cr_parallel");
            Directory.CreateDirectory(dir);

            string dest = Path.Combine(dir, "existing.jpg");
            await File.WriteAllBytesAsync(dest, new byte[] { 99, 98, 97 });

            var plan = new BackupPlan
            {
                ComparisonType = CollisionComparisonType.Binary,
                CollisionResolution = CollisionResolutionType.Rename,
                RenameStrategy = RenameStrategy.Increment
            };

            const int workerCount = 8;
            var resolver = BuildResolver();
            var tasks = new List<Task<CollisionResult>>();
            var sourceFiles = new List<string>();

            try
            {
                for(int i = 0; i < workerCount; i++)
                {
                    string src = Path.Combine(dir, $"source_{i}.jpg");
                    await File.WriteAllBytesAsync(src, new byte[] { (byte)i, (byte)(i + 1), (byte)(i + 2) });
                    sourceFiles.Add(src);

                    var item = CreateItemFromFile(src);
                    tasks.Add(resolver.ResolveAsync(item, dest, plan, CancellationToken.None));
                }

                CollisionResult[] results = await Task.WhenAll(tasks);
                var renamedTargets = results.Select(r => r.TargetPath).ToList();

                Assert.All(results, r => Assert.Equal(BackupActionType.Rename, r.Action));
                Assert.Equal(workerCount, renamedTargets.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                Assert.False(renamedTargets.Any(p => string.Equals(p, dest, StringComparison.OrdinalIgnoreCase)));
            }
            finally
            {
                foreach(string src in sourceFiles) TryDelete(src);
                TryDelete(dest);
                TryDeleteDir(dir);
            }
        }

        // ──────────────────────────────────────────────────────────
        // ComparisonType.None — always treats files as different
        // ──────────────────────────────────────────────────────────

        [Fact]
        public async Task ResolveAsync_ComparisonTypeNone_WhenDestinationExists_TreatsAsCollision()
        {
            // Write two *identical* files – with None comparison they should still be treated as different
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_cr_none");
            Directory.CreateDirectory(dir);
            string src  = Path.Combine(dir, "same_src.jpg");
            string dest = Path.Combine(dir, "same_dst.jpg");
            try
            {
                byte[] data = new byte[] { 10, 20, 30 };
                await File.WriteAllBytesAsync(src,  data);
                await File.WriteAllBytesAsync(dest, data);

                var item = CreateItemFromFile(src);
                var plan = new BackupPlan
                {
                    ComparisonType    = CollisionComparisonType.None,
                    CollisionResolution = CollisionResolutionType.Rename,
                    RenameStrategy    = RenameStrategy.Increment
                };

                var result = await BuildResolver().ResolveAsync(item, dest, plan, CancellationToken.None);

                // ComparisonType.None skips content check -> files treated as different -> Rename applied
                Assert.Equal(BackupActionType.Rename, result.Action);
            }
            finally
            {
                TryDelete(src); TryDelete(dest);
                TryDeleteDir(dir);
            }
        }

        // ──────────────────────────────────────────────────────────
        // Cleanup helpers
        // ──────────────────────────────────────────────────────────

        private static void TryDelete(string path)   { try { File.Delete(path); } catch { } }
        private static void TryDeleteDir(string dir) { try { Directory.Delete(dir); } catch { } }
    }
}
