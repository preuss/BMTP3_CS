using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.Services;
using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.Exceptions;
using BMTP3.Core4.State;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.CommandLine;

namespace BMTP3.Consoles.Tests;

public class BackupConsoleCommand4SessionVersionTests
{
	[Fact(Timeout = 30_000)]
	public async Task IncompatibleSessionFile_PrintsVersionsAndReturnsExit1()
	{
		StringWriter consoleOutput = new();

		ServiceCollection services = new();
		services.AddLogging();
		services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Create(new AnsiConsoleSettings
		{
			Ansi = AnsiSupport.No,
			ColorSystem = ColorSystemSupport.NoColors,
			Out = new AnsiConsoleOutput(consoleOutput)
		}));
		services.AddSingleton<IBackupEngine>(new SessionVersionThrowingEngine());

		ServiceProvider sp = services.BuildServiceProvider();

		BackupConsoleCommand4 command = new() { ServiceProvider = sp };
		RootCommand rootCommand = new("BMTP3 CLI - session version test");
		rootCommand.Subcommands.Add(command);

		ParseResult parseResult = rootCommand.Parse(
			"backup4 --source-type MediaDevice --source-path mtp://FakeDevice/InternalStorage --output D:\\out --name sessionversion");
		Assert.Empty(parseResult.Errors);

		int exitCode = await parseResult.InvokeAsync(null, CancellationToken.None);

		Assert.Equal(1, exitCode);

		string output = consoleOutput.ToString();
		Assert.Contains("version 1", output);
		Assert.Contains("version 2", output);
		Assert.Contains("not backward compatible", output);
	}

	private sealed class SessionVersionThrowingEngine : IBackupEngine
	{
		public Task<BackupResult> RunAsync(
			BackupPlan plan,
			IProgress<BackupProgress>? progress,
			CancellationToken cancellationToken)
		{
			throw new SessionVersionMismatchException(
				foundVersion: 1,
				expectedVersion: BackupSummary.CurrentVersion,
				sessionFile: new FileInfo("session.json"));
		}
	}
}
