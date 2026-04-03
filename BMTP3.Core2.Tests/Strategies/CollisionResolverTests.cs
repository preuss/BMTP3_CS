using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class CollisionResolverTests
    {
        private class SimplePathGenerator : IPathGenerator
        {
            public string ApplyPattern(string pattern, IBackupItem item) => Path.Combine(Path.GetDirectoryName(item.SourcePath) ?? string.Empty, Path.GetFileName(item.SourcePath));
            public string GenerateRelativePath(IBackupItem item, BackupPlan plan) => Path.GetFileName(item.SourcePath);
        }

        private class DummyMetadataReader : IMetadataReader { public Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct) => Task.CompletedTask; }

        private class FakeHashGenerator : IHashGenerator
        {
            public async Task<Dictionary<HashType, string>> ComputeHashesAsync(Stream stream, IEnumerable<HashType> hashTypes, IProgress<ulong> progress, CancellationToken ct)
            {
                // Compute SHA256 for any requested algorithm – tests will use SHA2_256
                using var sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(stream);
                string hex = string.Concat(hash.Select(b => b.ToString("x2")));
                var dict = new Dictionary<HashType, string>();
                foreach (var h in hashTypes) dict[h] = hex;
                return await Task.FromResult(dict);
            }
        }

        [Fact]
        public async Task ResolveAsync_WhenSidecarMissing_UsesHashGeneratorFallback()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_test_cr");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "file.jpg");
            string src = Path.Combine(dir, "src.jpg");
            try
            {
                byte[] data = new byte[8192];
                new Random(123).NextBytes(data);
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

                var resolver = new CollisionResolver(new DummyMetadataReader(), new SimplePathGenerator(), LoggerFactory.Create(b => { }).CreateLogger<CollisionResolver>(), new FakeHashGenerator());

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
        public async Task ResolveAsync_Uses_DestinationHashes_From_Item_Metadata_First()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_test_cr3");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "file3.jpg");
            string src = Path.Combine(dir, "src3.jpg");
            try
            {
                byte[] data = new byte[4096];
                new Random(99).NextBytes(data);
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

                // Pre-populate DestinationHashes with the same hash to simulate inspector
                var destHashes = new Dictionary<HashType, string> { { HashType.SHA2_256, shaHex } };
                item.Metadata.Set(MetadataKey.DestinationHashes, destHashes);

                var plan = new BackupPlan { ComparisonType = CollisionComparisonType.Hash };

                var resolver = new CollisionResolver(new DummyMetadataReader(), new SimplePathGenerator(), LoggerFactory.Create(b => { }).CreateLogger<CollisionResolver>(), null);

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
        public async Task ResolveAsync_WhenSidecarMissing_WithoutHashGenerator_FallsBackToBinaryCompare()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_test_cr2");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "file2.jpg");
            string src = Path.Combine(dir, "src2.jpg");
            try
            {
                byte[] data = new byte[4096];
                new Random(77).NextBytes(data);
                await File.WriteAllBytesAsync(dest, data);
                await File.WriteAllBytesAsync(src, data);

                IContent content = new FileContent(src);
                var item = BackupItem.Create(content, Path.GetFileName(src));
                // No hashes stored intentionally

                var plan = new BackupPlan { ComparisonType = CollisionComparisonType.Hash };

                var resolver = new CollisionResolver(new DummyMetadataReader(), new SimplePathGenerator(), LoggerFactory.Create(b => { }).CreateLogger<CollisionResolver>(), null);

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
    }
}
