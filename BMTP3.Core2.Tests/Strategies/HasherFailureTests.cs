using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Content;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class HasherFailureTests
    {
        private class SimplePathGenerator : IPathGenerator
        {
            public string ApplyPattern(string pattern, IBackupItem item) => Path.GetFileName(item.SourcePath);
            public string GenerateRelativePath(IBackupItem item, BackupPlan plan) => Path.GetFileName(item.SourcePath);
        }

        private class DummyMetadataReader : IMetadataReader { public Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct) => Task.CompletedTask; }

        private class ThrowingHashGenerator : IHashGenerator
        {
            public Task<Dictionary<HashType, string>> ComputeHashesAsync(Stream stream, IEnumerable<HashType> hashTypes, IProgress<ulong> progress, CancellationToken ct)
            {
                throw new InvalidOperationException("Simulated IHashGenerator failure");
            }
        }

        private class ThrowingDestinationInspector : BMTP3.Core2.BackupNew.Engine.Strategies.IDestinationInspector
        {
            public Task<BMTP3.Core2.BackupNew.Engine.Strategies.FileSnapshot> GetSnapshotAsync(string path, CancellationToken ct)
            {
                bool exists = File.Exists(path);
                ulong length = exists ? (ulong)new FileInfo(path).Length : 0UL;
                DateTime lastWrite = exists ? new FileInfo(path).LastWriteTimeUtc : default;
                return Task.FromResult(new BMTP3.Core2.BackupNew.Engine.Strategies.FileSnapshot
                {
                    Exists = exists,
                    Length = length,
                    LastWriteTimeUtc = lastWrite
                });
            }

            public Task<string> GetHashAsync(string path, string algorithm, CancellationToken ct)
            {
                throw new InvalidOperationException("Simulated IDestinationInspector.GetHashAsync failure");
            }
        }

        [Fact]
        public async Task CollisionResolver_FallsBackToBinary_When_HashGenerator_Throws()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_hasherfail_cr");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "file.jpg");
            string src = Path.Combine(dir, "src.jpg");
            try
            {
                byte[] data = new byte[8192];
                new Random(1234).NextBytes(data);
                await File.WriteAllBytesAsync(dest, data);
                await File.WriteAllBytesAsync(src, data);

                // Compute SHA256 hex
                string shaHex;
                using (var sha = SHA256.Create())
                using (var s = File.OpenRead(src))
                {
                    var h = sha.ComputeHash(s);
                    shaHex = string.Concat(h.Select(b => b.ToString("x2")));
                }

                IContent content = new FileContent(src);
                var item = BackupItem.Create(content, Path.GetFileName(src));
                var hashes = new Dictionary<HashType, string> { { HashType.SHA2_256, shaHex } };
                item.Metadata.Set(MetadataKey.Hashes, hashes);

                var plan = new BackupPlan { ComparisonType = CollisionComparisonType.Hash };

                var resolver = new CollisionResolver(new DummyMetadataReader(), new SimplePathGenerator(), LoggerFactory.Create(b => { }).CreateLogger<CollisionResolver>(), new ThrowingHashGenerator());

                var result = await resolver.ResolveAsync(item, dest, plan, CancellationToken.None);

                Assert.Equal(BackupActionType.Skip, result.Action);
            }
            finally
            {
                try { File.Delete(dest); } catch { }
                try { File.Delete(src); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }

        [Fact]
        public async Task DestinationInspector_DoesNotThrow_When_HashGenerator_Fails()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_hasherfail_di");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "file2.jpg");
            try
            {
                byte[] data = new byte[4096];
                new Random(4321).NextBytes(data);
                await File.WriteAllBytesAsync(dest, data);

                var plan = new BackupPlan { HashTypes = new HashSet<HashType> { HashType.SHA2_256 } };
                var step = new DestinationInspectorItemStep(new ThrowingDestinationInspector(), new ThrowingHashGenerator(), plan, LoggerFactory.Create(b => { }).CreateLogger<DestinationInspectorItemStep>());

                var content = new FileContent(dest);
                var item = BackupItem.Create(content, Path.GetFileName(dest));
                item.Metadata.Set(MetadataKey.FinalTargetPath, dest);

                // Should not throw
                await step.ExecuteAsync(item, new System.Progress<ulong>(), CancellationToken.None);

                var stored = item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.DestinationHashes);
                Assert.True(stored == null || stored.Count == 0);
            }
            finally
            {
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }
    }
}
