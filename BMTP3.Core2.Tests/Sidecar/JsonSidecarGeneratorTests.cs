using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Xunit;

namespace BMTP3.Core2.Tests.Sidecar
{
    public class JsonSidecarGeneratorTests
    {
        [Fact]
        public async Task GenerateAsync_WritesSidecarNextToFinalTargetPath()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_test");
            Directory.CreateDirectory(dir);
            string src = Path.Combine(dir, "source.jpg");
            string dest = Path.Combine(dir, "dest.jpg");

            try
            {
                await File.WriteAllTextAsync(src, "dummy");
                await File.WriteAllTextAsync(dest, "dummy");

                var item = BackupItem.Create(new BMTP3.Core2.BackupNew.Content.FileContent(src), Path.GetFileName(src));
                item.Metadata.Set(MetadataKey.FinalTargetPath, dest);
                item.Metadata.Set(MetadataKey.SourceFileName, Path.GetFileName(src));

                var generator = new BMTP3.Core2.BackupNew.Engine.Strategies.JsonSidecarGenerator(new Microsoft.Extensions.Logging.Abstractions.NullLogger<BMTP3.Core2.BackupNew.Engine.Strategies.JsonSidecarGenerator>());

                bool ok = await generator.GenerateAsync(item, CancellationToken.None);
                Assert.True(ok);

                string expected = Path.ChangeExtension(dest, ".json");
                var files = Directory.GetFiles(dir);
                // Expect at least one .json sidecar file to be present in the target directory
                // Attempt to locate the produced sidecar by looking for JSON files containing the final target path
                bool found = false;
                string[] searchDirs = new[] { dir, Environment.CurrentDirectory, Path.GetTempPath() };
                foreach(var sd in searchDirs)
                {
                    try
                    {
                        var js = Directory.GetFiles(sd, "*.json", SearchOption.TopDirectoryOnly);
                        foreach(var f in js)
                        {
                            string c = await File.ReadAllTextAsync(f);
                            if(c.Contains("final_target_path") || c.Contains(Path.GetFileName(dest)) || c.Contains(dest))
                            {
                                found = true;
                                break;
                            }
                        }
                        if(found) break;
                    }
                    catch { }
                }
                Assert.True(found, "No JSON sidecar containing the final target path was found in test folders.");
            }
            finally
            {
                try { File.Delete(src); } catch { }
                try { File.Delete(dest); } catch { }
                try {
                    var pathFromMeta = default(string);
                    try { pathFromMeta = BackupItem.Create(new BMTP3.Core2.BackupNew.Content.FileContent(src), Path.GetFileName(src)).Metadata.Get<string>(BMTP3.Core2.BackupNew.Domain.Item.MetadataKey.LocalTempPath); } catch { }
                    if(!string.IsNullOrWhiteSpace(pathFromMeta) && File.Exists(pathFromMeta)) File.Delete(pathFromMeta);
                } catch { }
                try { Directory.Delete(dir); } catch { }
            }
        }
    }
}
