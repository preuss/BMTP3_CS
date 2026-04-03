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
using BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;
using BMTP3.Core2.BackupNew.Domain.Item;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class DestinationInspectorTests
    {
        private class FakeHashGenerator : IHashGenerator
        {
            public async Task<Dictionary<HashType, string>> ComputeHashesAsync(Stream stream, IEnumerable<HashType> hashTypes, IProgress<ulong> progress, CancellationToken ct)
            {
                using var sha = SHA256.Create();
                var h = sha.ComputeHash(stream);
                string hex = string.Concat(h.Select(b => b.ToString("x2")));
                var dict = new Dictionary<HashType, string>();
                foreach (var ht in hashTypes) dict[ht] = hex;
                return await Task.FromResult(dict);
            }
        }

        private class FakeDestinationInspector : BMTP3.Core2.BackupNew.Engine.Strategies.IDestinationInspector
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

            public async Task<string> GetHashAsync(string path, string algorithm, CancellationToken ct)
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(path);
                var h = sha.ComputeHash(stream);
                return await Task.FromResult(string.Concat(h.Select(b => b.ToString("x2"))));
            }
        }

        [Fact]
        public async Task Inspector_Computes_And_Stores_DestinationHashes_When_No_Sidecar()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_destinsp");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, "file.jpg");
            try
            {
                byte[] data = new byte[4096];
                new Random(42).NextBytes(data);
                await File.WriteAllBytesAsync(dest, data);

                var plan = new BackupPlan();
                var step = new DestinationInspectorItemStep(new FakeDestinationInspector(), new FakeHashGenerator(), plan, LoggerFactory.Create(b => { }).CreateLogger<DestinationInspectorItemStep>());

                var content = new BMTP3.Core2.BackupNew.Content.FileContent(dest);
                var item = BackupItem.Create(content, Path.GetFileName(dest));
                item.Metadata.Set(MetadataKey.FinalTargetPath, dest);

                await step.ExecuteAsync(item, new Progress<ulong>(), CancellationToken.None);

                var stored = item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.DestinationHashes);
                Assert.NotNull(stored);
                Assert.True(stored.ContainsKey(HashType.SHA2_256) || stored.Count > 0);
            }
            finally
            {
                try { File.Delete(dest); } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }
    }
}
