using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class CollisionResolver_MismatchTests
    {
        private class SimplePathGenerator : IPathGenerator
        {
            public string ApplyPattern(string pattern, IBackupItem item) => Path.GetFileName(item.SourcePath);
            public string GenerateRelativePath(IBackupItem item, BackupPlan plan) => Path.GetFileName(item.SourcePath);
        }

        private class DummyMetadataReader : IMetadataReader { public Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct) => Task.CompletedTask; }

        [Fact]
        public async Task ResolveAsync_WhenSidecarHashDiffers_ReturnsRename()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_cr_mismatch");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "file.jpg");
            string destSidecar = dest + ".bmtp3.json";
            string src = Path.Combine(dir, "src.jpg");
            try
            {
                byte[] srcData = new byte[2048]; new Random(1).NextBytes(srcData);
                byte[] destData = new byte[2048]; new Random(2).NextBytes(destData);
                await File.WriteAllBytesAsync(src, srcData);
                await File.WriteAllBytesAsync(dest, destData);

                // compute src hash
                string srcHex;
                using (var sha = SHA256.Create()) using (var s = File.OpenRead(src)) { srcHex = string.Concat(sha.ComputeHash(s).Select(b => b.ToString("x2"))); }

                // write dest sidecar with a different (fake) hash value
                var sidecarJson = "{\"hashes\": { \"SHA2_256\": \"deadbeef\" } }";
                await File.WriteAllTextAsync(destSidecar, sidecarJson);

                var item = BackupItem.Create(new BMTP3.Core2.BackupNew.Content.FileContent(src), Path.GetFileName(src));
                item.Metadata.Set(MetadataKey.Hashes, new Dictionary<HashType, string> { { HashType.SHA2_256, srcHex } });

                var plan = new BackupPlan { ComparisonType = CollisionComparisonType.Hash, CollisionResolution = CollisionResolutionType.Rename };

                var resolver = new CollisionResolver(new DummyMetadataReader(), new SimplePathGenerator(), LoggerFactory.Create(b => { }).CreateLogger<CollisionResolver>(), null);

                var result = await resolver.ResolveAsync(item, dest, plan, CancellationToken.None);

                Assert.Equal(BMTP3.Core2.BackupNew.Api.Enums.BackupActionType.Rename, result.Action);
            }
            finally
            {
                try { File.Delete(dest); } catch { }
                try { File.Delete(src); } catch { }
                try { File.Delete(destSidecar); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }
    }
}
