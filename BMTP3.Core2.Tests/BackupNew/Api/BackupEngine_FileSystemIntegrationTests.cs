using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace BMTP3.Core2.Tests.BackupNew.Api;
public class BackupEngine_FileSystemIntegrationTests
{
	private readonly ITestOutputHelper _output;

	public BackupEngine_FileSystemIntegrationTests(ITestOutputHelper output) => _output = output;

	[Fact(Timeout = 60_000)]
	[Trait("Category", "Integration")]
	public async Task RunAsync_WithRealBackupScanner_ProcessesFiles()
	{
		var services = new ServiceCollection();

		// Route logs into xUnit output (reuse your provider)
		services.AddLogging(lb => lb.AddProvider(new XunitTestOutputLoggerProvider(_output)));

		// Register BMTP3 with a preConfigure to ensure the high-level scanner is the real BackupScanner.
		services.AddBMTP3Core2(s => s.AddSingleton<IBackupScanner, BackupScanner>());
		// Enable single-threaded debug mode for deterministic debugging
		services.Configure<BackupEngineOptions>(o => o.DebugSingleThreaded = true);

		var sp = services.BuildServiceProvider();
		var engine = sp.GetRequiredService<IBackupEngine>();

		// Use TestData folder copied to test output as source template
		string dataRoot = Path.Combine(AppContext.BaseDirectory, "TestData");
		_output.WriteLine($"TestData path: {dataRoot}");
		Assert.True(Directory.Exists(dataRoot), $"TestData not found at {dataRoot}");

		// Log alle filer for diagnostic
		foreach (var f in Directory.EnumerateFiles(dataRoot, "*", SearchOption.AllDirectories))
		{
			_output.WriteLine($"  {f}");
		}

		// Copy full TestData folder to a temporary source directory that the engine will scan
		var tempSource = Path.Combine(Path.GetTempPath(), "bmtp3-src", Guid.NewGuid().ToString("N"));
		CopyDirectory(dataRoot, tempSource);

		// Create temporary output dir required by JobValidator
		var tempOutput = Path.Combine(Path.GetTempPath(), "bmtp3-out", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempOutput);

		var plan = new BackupPlan
		{
			Name = "fs-integration",
			SourceType = SourceType.FileSystem,
			SourcePath = tempSource,    // <- point the engine at the copied TestData folder
			SourceId = string.Empty,
			Recursive = true,
			OutputPath = tempOutput
		};

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
		var progress = new Progress<IBackupProgress>(p => { /* optional inspect */ });

		try
		{
			var result = await engine.RunAsync(plan, progress, cts.Token);

			Assert.NotNull(result);
			Assert.Equal(JobState.Completed, result.Status);
			Assert.True(result.TotalFilesScanned >= 1, "Expected at least one file discovered.");
		} finally
		{
			try { Directory.Delete(tempSource, true); } catch { }
			try { Directory.Delete(tempOutput, true); } catch { }
		}
	}

	// helper method (put inside the test class)
	private static void CopyDirectory(string sourceDir, string targetDir)
	{
		Directory.CreateDirectory(targetDir);
		foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
		{
			var dest = Path.Combine(targetDir, Path.GetFileName(file));
			File.Copy(file, dest, overwrite: true);
		}
		foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
		{
			var destSub = Path.Combine(targetDir, Path.GetFileName(dir));
			CopyDirectory(dir, destSub);
		}
	}
}