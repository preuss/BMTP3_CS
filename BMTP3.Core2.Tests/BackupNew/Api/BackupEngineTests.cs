using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Engine.Traversal;

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
		var services = new ServiceCollection();

		// Route logs into xUnit output so you can see them in Test Explorer
		services.AddLogging(lb => lb.AddProvider(new XunitTestOutputLoggerProvider(_output)));

		// Register production DI defaults from the library
		services.AddSingleton<IBackupScanner, NoopBackupScanner>();
		services.AddBMTP3Core2();

		var sp = services.BuildServiceProvider();
		var engine = sp.GetRequiredService<IBackupEngine>();

		// Create a temporary output folder so JobValidator has a valid OutputPath
		var tempOutput = Path.Combine(Path.GetTempPath(), "bmtp3-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempOutput);

		var plan = new BackupPlan
		{
			Name = "unit-test-no-files",
			SourceType = SourceType.FileSystem,
			SourcePath = "C:\\nonexistent-path-for-test",
			SourceId = string.Empty,
			Recursive = true,
			OutputPath = tempOutput
		};

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var progress = new Progress<IBackupProgress>(p => { /* optional: inspect progress */ });

		try
		{
			var result = await engine.RunAsync(plan, progress, cts.Token);

			Assert.NotNull(result);
			Assert.Equal(JobState.Completed, result.Status);
			Assert.Equal(0, result.TotalFilesScanned);
		}
		finally
		{
			try
			{
				Directory.Delete(tempOutput, true);
			}
			catch
			{
				// Best-effort cleanup for test artifacts
			}
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

		public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			try
			{
				_output.WriteLine($"[{logLevel}] {_category}: {formatter(state, exception)}");
				if (exception != null) _output.WriteLine(exception.ToString());
			}
			catch { } // test output should not throw tests
		}

		private class NullScope : IDisposable
		{
			public static NullScope Instance { get; } = new NullScope();
			public void Dispose() { }
		}
	}
}