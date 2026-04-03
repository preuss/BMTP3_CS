using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class DestinationInspectorCacheTests
    {
        private class TestInspector : DestinationInspector
        {
            public int ComputeCount = 0;

            public TestInspector(TimeSpan ttl) : base(ttl) { }

            protected override Task<string> ComputeFileHashAsync(string filePath, string algorithm, CancellationToken ct)
            {
                ComputeCount++;
                // return a deterministic fake hash
                return Task.FromResult("deadbeef");
            }
        }

        [Fact]
        public async Task GetHashAsync_UsesCache_Within_TTL()
        {
            var inspector = new TestInspector(TimeSpan.FromSeconds(5));
            string path = "C:\\fake\\file.jpg";
            string algo = "SHA256";

            string a = await inspector.GetHashAsync(path, algo, CancellationToken.None);
            string b = await inspector.GetHashAsync(path, algo, CancellationToken.None);

            Assert.Equal(a, b);
            Assert.Equal(1, inspector.ComputeCount);
        }

        [Fact]
        public async Task GetHashAsync_Expires_After_TTL()
        {
            var inspector = new TestInspector(TimeSpan.FromMilliseconds(100));
            string path = "C:\\fake\\file2.jpg";
            string algo = "SHA256";

            string a = await inspector.GetHashAsync(path, algo, CancellationToken.None);
            await Task.Delay(200);
            string b = await inspector.GetHashAsync(path, algo, CancellationToken.None);

            Assert.Equal(a, b);
            Assert.Equal(2, inspector.ComputeCount);
        }

        [Fact]
        public async Task GetHashAsync_Concurrent_Calls_Compute_Once()
        {
            var inspector = new DestinationInspectorCacheTests.TestInspector(TimeSpan.FromSeconds(5));
            string path = "C:\\fake\\file3.jpg";
            string algo = "SHA256";

            var tasks = new List<Task<string>>();
            for(int i = 0; i < 8; i++) tasks.Add(inspector.GetHashAsync(path, algo, CancellationToken.None));

            var results = await Task.WhenAll(tasks);

            Assert.All(results, r => Assert.Equal("deadbeef", r));
            Assert.Equal(1, inspector.ComputeCount);
        }

        [Fact]
        public async Task GetHashAsync_Accepts_SHA2_256_Alias()
        {
            string file = Path.Combine(Path.GetTempPath(), $"bmtp3_destinsp_alias_{Guid.NewGuid():N}.bin");
            try
            {
                await File.WriteAllBytesAsync(file, new byte[] { 1, 2, 3, 4, 5 });
                var inspector = new DestinationInspector(TimeSpan.FromMinutes(1));

                string hashFromAlias = await inspector.GetHashAsync(file, "SHA2_256", CancellationToken.None);
                string hashFromCanonical = await inspector.GetHashAsync(file, "SHA256", CancellationToken.None);

                Assert.False(string.IsNullOrWhiteSpace(hashFromAlias));
                Assert.Equal(hashFromCanonical, hashFromAlias);
            }
            finally
            {
                try { if(File.Exists(file)) File.Delete(file); } catch { }
            }
        }
    }
}
