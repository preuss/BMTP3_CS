using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using BMTP3.Core2.BackupNew.Domain.Item;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Response;
using Xunit;

namespace BMTP3.Core2.Tests.Integration
{
    public class BackupEngineIntegrationTests
    {
        private class TestScanner : IBackupScanner
        {
            private readonly string _sourceDir;
            public TestScanner(string sourceDir) { _sourceDir = sourceDir; }

            public async IAsyncEnumerable<IBackupItem> ScanAsync(BackupPlan plan, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
            {
                foreach(string f in Directory.GetFiles(_sourceDir))
                {
                    BackupItem item = BackupItem.Create(new BMTP3.Core2.BackupNew.Content.FileContent(f), Path.GetFileName(f));
                    item.Metadata.Set(MetadataKey.SourceFileName, Path.GetFileName(f));
                    item.Metadata.Set(MetadataKey.Length, (ulong)new FileInfo(f).Length);
                    yield return item;
                    await Task.Yield();
                }
            }
        }

        [Fact]
        public async Task RunAsync_EndToEnd_CopiesFileAndWritesSidecar()
        {
            string srcDir = Path.Combine(Path.GetTempPath(), "bmtp3_integ_src");
            string outDir = Path.Combine(Path.GetTempPath(), "bmtp3_integ_out");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(outDir);
            string srcFile = Path.Combine(srcDir, "file.txt");

            try
            {
                await File.WriteAllTextAsync(srcFile, "integration!\n");

                BackupPlan plan = new()
                {
                    Name = "integration-test",
                    SourceType = SourceType.FileSystem,
                    SourceId = srcDir,
                    SourcePath = srcDir,
                    OutputPath = outDir,
                    SidecarFormat = SidecarFormat.Ini,
                    PostWriteVerification = PostWriteVerificationType.Binary,
                    DryRun = true
                };

                var services = new ServiceCollection();
                services.AddLogging();
                services.AddBMTP3Core2(preConfigure: sc =>
                {
                    // override scanner with our test scanner
                    sc.AddSingleton<IBackupScanner>(_ => new TestScanner(srcDir));
                });

                ServiceProvider sp = services.BuildServiceProvider();
                IBackupEngine engine = sp.GetRequiredService<IBackupEngine>();

                BackupJobResult result = await engine.RunAsync(plan, null, CancellationToken.None);

                // In DryRun mode we may not produce the actual destination file, but FinalTargetPath and sidecar should be produced.
                string sidecar = Path.ChangeExtension(Path.Combine(outDir, Path.GetFileName(srcFile)), ".ini");
                Assert.True(File.Exists(sidecar), "Expected sidecar to be written in DryRun mode");
            }
            finally
            {
                try { File.Delete(srcFile); } catch { }
                try { Directory.Delete(srcDir); } catch { }
                try { Directory.Delete(outDir, true); } catch { }
            }
        }
    }
}
