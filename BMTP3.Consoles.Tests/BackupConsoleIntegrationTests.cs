using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.Services;
using BMTP3.Core2.BackupNew.DependencyInjection;
using BMTP3.Core2.BackupNew.Engine.Orchestration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Domain.Job;

namespace BMTP3.Consoles.Tests;
public class BackupConsoleIntegrationTests
{
    private readonly ITestOutputHelper _output;
    public BackupConsoleIntegrationTests(ITestOutputHelper output) => _output = output;

    [Fact(Timeout = 60_000)]
    [Trait("Category", "Integration")]
    public async Task BackupCommand_InvokedViaRootCommand_CompletesSuccessfully()
	{
		ServiceCollection services = new();
		services.AddLogging(lb => lb.AddProvider(new XunitTestOutputLoggerProvider(_output)));

        // Console services used by the command
        services.AddSingleton<ConsolesPrinter>();

        // Register core engine with real filesystem scanner and relaxed validator for CI
        services.AddBMTP3Core2(s =>
        {
            s.AddSingleton<IBackupScanner, BackupScanner>();
            s.AddSingleton<IJobValidator, RelaxedJobValidator>();
        });
		services.Configure<BackupEngineOptions>(o => o.DebugSingleThreaded = true);

		ServiceProvider sp = services.BuildServiceProvider();
		// Quick sanity: ensure engine is resolvable from DI (helps catch DI issues early)
		IBackupEngine? engineProbe = sp.GetService<IBackupEngine>();
		_output.WriteLine($"Engine resolved from DI: {engineProbe != null}");
        Assert.NotNull(engineProbe);

		// Prepare a small temporary source folder with a couple of files
		string tempSource = Path.Combine(Path.GetTempPath(), "bmtp3-src", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempSource);
        File.WriteAllText(Path.Combine(tempSource, "A.txt"), "hello A");
        Directory.CreateDirectory(Path.Combine(tempSource, "Sub"));
        File.WriteAllText(Path.Combine(tempSource, "Sub", "B.txt"), "hello B");

		string tempOutput = Path.Combine(Path.GetTempPath(), "bmtp3-out", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempOutput);

        // SourceId required by JobValidator: use root of tempSource
        string sourceId = Path.GetPathRoot(tempSource) ?? tempSource;

        try
        {
			// Build CLI and attach the command (we'll call TryRunAsync directly for deterministic assertions)
			RootCommand rootCommand = new("BMTP3 CLI - integration test");

			BackupConsoleCommand2 backupCommand = new() { ServiceProvider = sp };
			rootCommand.Subcommands.Add(backupCommand);

			using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

			// Build a BackupPlan identical to what the CLI would construct
			BackupPlan plan = new()
			{
				Name = "test-console-engine",
				SourceType = Core2.BackupNew.Api.Request.Enums.SourceType.FileSystem,
                SourcePath = tempSource,
                SourceId = sourceId,
                OutputPath = tempOutput,
                Recursive = true
            };

			Progress<Core2.BackupNew.Api.Progress.IBackupProgress> progress = new(p => _output.WriteLine($"{p.Phase}: discovered={p.FilesDiscovered} succeeded={p.FilesSucceeded} failed={p.FilesFailed}"));

			// Call the programmatic helper on the command to get a structured BackupJobResult
			BackupConsoleCommand2 command = backupCommand;
			BackupJobResult? result = await command.TryRunAsync(plan, progress, cts.Token);

			_output.WriteLine($"Command run status: {result?.Status}. Files scanned: {result?.TotalFilesScanned}");
			Assert.NotNull(result);
			Assert.Equal(JobState.Completed, result.Status);

            // Ensure something was written to output
            Assert.True(Directory.EnumerateFiles(tempOutput, "*", SearchOption.AllDirectories).Any(), "Expected files in output directory");
        }
        finally
        {
            try { Directory.Delete(tempSource, true); } catch { }
            try { Directory.Delete(tempOutput, true); } catch { }
        }
    }

    // helper methods

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
		foreach(string file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
		{
			string dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }
		foreach(string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            string destSub = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSub);
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
                if (exception != null) _output.WriteLine(exception.ToString());
            }
            catch { }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}
