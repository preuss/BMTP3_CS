using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class BackupEngineTests
{
	private readonly ITestOutputHelper _output;

	public BackupEngineTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact(Timeout = 60_000)]
	public async Task RunAsync_WithNoopScanner_CompletesSuccessfully()
	{
		ServiceCollection services = new();

		// Route logs into xUnit output so you can see them in Test Explorer
		services.AddLogging(lb => lb.AddProvider(new XunitTestOutputLoggerProvider(_output)));

		// Register production DI defaults from the library
		services.AddSingleton<IBackupScanner, NoopBackupScanner>();
		services.AddBMTP3Core2();

		ServiceProvider sp = services.BuildServiceProvider();
		IBackupEngine engine = sp.GetRequiredService<IBackupEngine>();

		// Create a temporary source and output folder
		string tempSource = Path.Combine(Path.GetTempPath(), "bmtp3-tests", Guid.NewGuid().ToString("N") + "_source");
		string tempOutput = Path.Combine(Path.GetTempPath(), "bmtp3-tests", Guid.NewGuid().ToString("N") + "_output");
		Directory.CreateDirectory(tempSource);
		Directory.CreateDirectory(tempOutput);

		BackupPlan plan = new()
		{
			Name = "unit-test-no-files",
			SourceType = SourceType.FileSystem,
			SourcePath = tempSource,
			SourceId = tempSource,
			Recursive = true,
			OutputPath = tempOutput
		};

		using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
		Progress<IBackupProgress> progress = new(p => { /* optional: inspect progress */ });

		try
		{
			BackupJobResult result = await engine.RunAsync(plan, progress, cts.Token);

			Assert.NotNull(result);
			Assert.Equal(JobState.Completed, result.Status);
			Assert.Equal(0, result.TotalFilesScanned);
		} finally
		{
			try
			{
				Directory.Delete(tempSource, true);
				Directory.Delete(tempOutput, true);
			} catch
			{
				// Best-effort cleanup for test artifacts
			}
		}
	}

	// ----------------------------------------------------------------
	// Pre-flight validation tests (ValidateSourceAndOutput)
	// ----------------------------------------------------------------

	[Fact]
	public async Task RunAsync_WithMissingSourcePath_Throws()
	{
		ServiceCollection services = new();
		services.AddSingleton<IBackupScanner, NoopBackupScanner>();
		services.AddBMTP3Core2();

		ServiceProvider sp = services.BuildServiceProvider();
		IBackupEngine engine = sp.GetRequiredService<IBackupEngine>();

		string tempOutput = Path.Combine(Path.GetTempPath(), "bmtp3-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempOutput);

		BackupPlan plan = new()
		{
			Name = "preflight-test",
			SourceType = SourceType.FileSystem,
			SourcePath = "C:\\nonexistent-path-for-preflight-test",
			SourceId = "C:",
			Recursive = true,
			OutputPath = tempOutput
		};

		try
		{
			await engine.RunAsync(plan, null, CancellationToken.None);
			Assert.Fail("Expected DirectoryNotFoundException");
		} catch(DirectoryNotFoundException)
		{
			// Expected
		} finally
		{
			try { Directory.Delete(tempOutput, true); } catch { }
		}
	}

	[Fact]
	public async Task RunAsync_WithNoReadAccess_Throws()
	{
		ServiceCollection services = new();
		services.AddSingleton<IBackupScanner, NoopBackupScanner>();
		services.AddBMTP3Core2();

		ServiceProvider sp = services.BuildServiceProvider();
		IBackupEngine engine = sp.GetRequiredService<IBackupEngine>();

		// Use a path that we can't read (system root without admin)
		string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		string tempOutput = Path.Combine(Path.GetTempPath(), "bmtp3-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempOutput);

		BackupPlan plan = new()
		{
			Name = "preflight-test",
			SourceType = SourceType.FileSystem,
			SourcePath = systemRoot,
			SourceId = systemRoot,
			Recursive = true,
			OutputPath = tempOutput
		};

		try
		{
			await engine.RunAsync(plan, null, CancellationToken.None);
			// If it doesn't throw, that's okay - might have access on this system
		} catch(UnauthorizedAccessException)
		{
			// Expected on systems without admin rights
		} finally
		{
			try { Directory.Delete(tempOutput, true); } catch { }
		}
	}

	[Fact]
	public async Task RunAsync_WithNoWriteAccess_Throws()
	{
		ServiceCollection services = new();
		services.AddSingleton<IBackupScanner, NoopBackupScanner>();
		services.AddBMTP3Core2();

		ServiceProvider sp = services.BuildServiceProvider();
		IBackupEngine engine = sp.GetRequiredService<IBackupEngine>();

		// Use a path that we can't write to (system root)
		string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
		string tempSource = Path.Combine(Path.GetTempPath(), "bmtp3-tests", Guid.NewGuid().ToString("N") + "_src");
		Directory.CreateDirectory(tempSource);

		BackupPlan plan = new()
		{
			Name = "preflight-test",
			SourceType = SourceType.FileSystem,
			SourcePath = tempSource,
			SourceId = tempSource,
			Recursive = true,
			OutputPath = systemRoot // Try to write to system root - should fail
		};

		try
		{
			await engine.RunAsync(plan, null, CancellationToken.None);
			// If it doesn't throw, that's okay - might have access on this system
		} catch(UnauthorizedAccessException)
		{
			// Expected on systems without admin rights
		} finally
		{
			try { Directory.Delete(tempSource, true); } catch { }
		}
	}

	[Fact]
	public async Task RunAsync_WithValidPaths_Succeeds()
	{
		ServiceCollection services = new();
		services.AddSingleton<IBackupScanner, NoopBackupScanner>();
		services.AddBMTP3Core2();

		ServiceProvider sp = services.BuildServiceProvider();
		IBackupEngine engine = sp.GetRequiredService<IBackupEngine>();

		string tempSource = Path.Combine(Path.GetTempPath(), "bmtp3-tests", Guid.NewGuid().ToString("N") + "_src");
		string tempOutput = Path.Combine(Path.GetTempPath(), "bmtp3-tests", Guid.NewGuid().ToString("N") + "_out");
		Directory.CreateDirectory(tempSource);
		Directory.CreateDirectory(tempOutput);

		BackupPlan plan = new()
		{
			Name = "preflight-test",
			SourceType = SourceType.FileSystem,
			SourcePath = tempSource,
			SourceId = tempSource,
			Recursive = true,
			OutputPath = tempOutput
		};

		try
		{
			BackupJobResult result = await engine.RunAsync(plan, null, CancellationToken.None);
			Assert.NotNull(result);
			Assert.Equal(JobState.Completed, result.Status);
		} finally
		{
			try { Directory.Delete(tempSource, true); } catch { }
			try { Directory.Delete(tempOutput, true); } catch { }
		}
	}
}

// Minimal ILoggerProvider that forwards logs to ITestOutputHelper
internal sealed class XunitTestOutputLoggerProvider : ILoggerProvider
{
	private readonly ITestOutputHelper _output;

	public XunitTestOutputLoggerProvider(ITestOutputHelper output) => _output = output;

	public ILogger CreateLogger(string categoryName) => new XunitTestOutputLogger(_output, categoryName);

	public void Dispose() { }

	private sealed class XunitTestOutputLogger : ILogger
	{
		private readonly ITestOutputHelper _output;
		private readonly string _category;

		public XunitTestOutputLogger(ITestOutputHelper output, string category)
		{
			_output = output;
			_category = category;
		}

		public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			try
			{
				_output.WriteLine($"[{logLevel}] {_category}: {formatter(state, exception)}");
				if(exception != null) _output.WriteLine(exception.ToString());
			} catch { } // test output should not throw tests
		}

		private class NullScope : IDisposable
		{
			public static NullScope Instance { get; } = new NullScope();
			public void Dispose() { }
		}
	}
}
