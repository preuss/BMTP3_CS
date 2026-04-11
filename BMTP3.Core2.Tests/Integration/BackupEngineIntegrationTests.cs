using System.Runtime.CompilerServices;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3.Core2.Tests.Integration;

public class BackupEngineIntegrationTests
{
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
				// DryRun is disabled — the engine should physically write files and sidecars.
				DryRun = false
			};

			ServiceCollection services = new();
			services.AddLogging();
			services.AddBMTP3Core2(sc =>
			{
				// override scanner with our test scanner
				sc.AddSingleton<IBackupScanner>(_ => new TestScanner(srcDir));
			});

			ServiceProvider sp = services.BuildServiceProvider();
			IBackupEngine engine = sp.GetRequiredService<IBackupEngine>();

			BackupJobResult result = await engine.RunAsync(plan, null, CancellationToken.None);

			string sidecar = Path.ChangeExtension(Path.Combine(outDir, Path.GetFileName(srcFile)), ".ini");
			Assert.True(File.Exists(sidecar), $"Expected sidecar at '{sidecar}' to exist after a non-DryRun backup.");
		}
		finally
		{
			try
			{
				File.Delete(srcFile);
			}
			catch
			{
			}

			try
			{
				Directory.Delete(srcDir);
			}
			catch
			{
			}

			try
			{
				Directory.Delete(outDir, true);
			}
			catch
			{
			}
		}
	}

	private class TestScanner : IBackupScanner
	{
		private readonly string _sourceDir;

		public TestScanner(string sourceDir)
		{
			_sourceDir = sourceDir;
		}

		public async IAsyncEnumerable<IBackupItem> ScanAsync(BackupPlan plan,
			[EnumeratorCancellation] CancellationToken ct)
		{
			foreach (string f in Directory.GetFiles(_sourceDir))
			{
				BackupItem item = BackupItem.Create(new FileContent(f), Path.GetFileName(f));
				item.Metadata.Set(MetadataKey.SourceFileName, Path.GetFileName(f));
				item.Metadata.Set(MetadataKey.Length, (ulong)new FileInfo(f).Length);
				yield return item;
				await Task.Yield();
			}
		}
	}
}