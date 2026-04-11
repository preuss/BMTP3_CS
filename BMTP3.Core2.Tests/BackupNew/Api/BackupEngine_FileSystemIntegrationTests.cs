using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.Tests.BackupNew.Api;

public class BackupEngineFileSystemIntegrationTests
{
	private readonly ITestOutputHelper _output;

	public BackupEngineFileSystemIntegrationTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact(Timeout = 60_000)]
	[Trait("Category", "Integration")]
	public async Task RunAsync_WithRealBackupScanner_ProcessesFiles()
	{
		ServiceCollection services = new();

		// Route logs into xUnit output (reuse your provider)
		services.AddLogging(lb => lb.AddProvider(new XunitTestOutputLoggerProvider(_output)));

		// Register BMTP3 with a preConfigure to ensure the high-level scanner is the real BackupScanner.
		services.AddBMTP3Core2(s => s.AddSingleton<IBackupScanner, BackupScanner>());
		// Enable single-threaded debug mode for deterministic debugging
		services.Configure<BackupEngineOptions>(o => o.DebugSingleThreaded = true);

		ServiceProvider sp = services.BuildServiceProvider();
		IBackupEngine engine = sp.GetRequiredService<IBackupEngine>();

		// Use TestData folder copied to test output as source template
		string dataRoot = Path.Combine(AppContext.BaseDirectory, "TestData");
		_output.WriteLine($"TestData path: {dataRoot}");
		Assert.True(Directory.Exists(dataRoot), $"TestData not found at {dataRoot}");

		// Log alle filer for diagnostic
		foreach (string f in Directory.EnumerateFiles(dataRoot, "*", SearchOption.AllDirectories))
		{
			_output.WriteLine($"  {f}");
		}

		// Copy full TestData folder to a temporary source directory that the engine will scan
		string tempSource = Path.Combine(Path.GetTempPath(), "bmtp3-src", Guid.NewGuid().ToString("N"));
		CopyDirectory(dataRoot, tempSource);

		// Create temporary output dir required by JobValidator
		string tempOutput = Path.Combine(Path.GetTempPath(), "bmtp3-out", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempOutput);

		BackupPlan plan = new()
		{
			Name = "fs-integration",
			SourceType = SourceType.FileSystem,
			SourcePath = tempSource, // <- point the engine at the copied TestData folder
			// SourceId required by JobValidator; use root path of the tempSource
			SourceId = Path.GetPathRoot(tempSource) ?? tempSource,
			Recursive = true,
			OutputPath = tempOutput
		};

		using CancellationTokenSource cts = new(TimeSpan.FromSeconds(20));
		Progress<IBackupProgress> progress = new(p =>
		{
			/* optional inspect */
		});

		try
		{
			BackupJobResult result = await engine.RunAsync(plan, progress, cts.Token);

			Assert.NotNull(result);
			Assert.Equal(JobState.Completed, result.Status);
			Assert.True(result.TotalFilesScanned >= 1, "Expected at least one file discovered.");
		}
		finally
		{
			try
			{
				Directory.Delete(tempSource, true);
			}
			catch
			{
			}

			try
			{
				Directory.Delete(tempOutput, true);
			}
			catch
			{
			}
		}
	}

	// helper method (put inside the test class)
	private static void CopyDirectory(string sourceDir, string targetDir)
	{
		Directory.CreateDirectory(targetDir);
		foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
		{
			string dest = Path.Combine(targetDir, Path.GetFileName(file));
			File.Copy(file, dest, true);
		}

		foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
		{
			string destSub = Path.Combine(targetDir, Path.GetFileName(dir));
			CopyDirectory(dir, destSub);
		}
	}
}