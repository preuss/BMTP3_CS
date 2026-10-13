using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.ConsoleCommands.Core4;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace BMTP3.Consoles.Tests;

public class BackupConsoleHelpTests
{
	private static readonly ServiceProvider EmptyServiceProvider = new ServiceCollection().BuildServiceProvider();

	[Fact(Timeout = 30_000)]
	public async Task Backup4Help_ShowsDefaultsForAllOptions()
	{
		StringWriter consoleOutput = new();

		BackupConsoleCommand4 command = new() { ServiceProvider = EmptyServiceProvider };
		RootCommand rootCommand = new("BMTP3 CLI");
		rootCommand.Subcommands.Add(command);
		ConsolesProgram.ReplaceHelp(rootCommand, new CustomHelpAction());

		int exitCode = await rootCommand.Parse("backup4 --help")
			.InvokeAsync(new InvocationConfiguration { Output = consoleOutput }, CancellationToken.None);

		Assert.Equal(0, exitCode);

		string output = consoleOutput.ToString();

		// 3.x parity: bool defaults render as lowercase "true"; false stays hidden (like 3.x).
		Assert.Contains("[default: true]", output);
		Assert.DoesNotContain("[default: True]", output);
		Assert.DoesNotContain("[default: False]", output);
		Assert.DoesNotContain("[default: false]", output);

		// Enum and int options still render.
		Assert.Contains("[default: PreserveHierarchy]", output);
		Assert.Contains("[default: Abort]", output);
		Assert.Contains("[default: Persistent]", output);
		Assert.Contains("[default: 0]", output);

		// Standard sections and entries present.
		Assert.Contains("Usage:", output);
		Assert.Contains("[options]", output);
		Assert.Contains("-v, --verbose", output);
		Assert.Contains("-?, -h, --help", output);
	}

	[Fact(Timeout = 30_000)]
	public async Task RootHelp_ShowsUsageAndAllCommands()
	{
		StringWriter consoleOutput = new();

		RootCommand rootCommand = new("BMTP3 CLI");
		rootCommand.Subcommands.Add(new BackupConsoleCommand2() { ServiceProvider = EmptyServiceProvider });
		rootCommand.Subcommands.Add(new BackupTestConsoleCommand() { ServiceProvider = EmptyServiceProvider });
		rootCommand.Subcommands.Add(new VerifyConsoleCommand() { ServiceProvider = EmptyServiceProvider });
		rootCommand.Subcommands.Add(new BackupConsoleCommand4() { ServiceProvider = EmptyServiceProvider });
		rootCommand.Subcommands.Add(new BackupConsoleCommand4ListDrives() { ServiceProvider = EmptyServiceProvider });
		rootCommand.Subcommands.Add(new BackupConsoleCommand4InitConfig() { ServiceProvider = EmptyServiceProvider });
		ConsolesProgram.ReplaceHelp(rootCommand, new CustomHelpAction());

		int exitCode = await rootCommand.Parse("--help")
			.InvokeAsync(new InvocationConfiguration { Output = consoleOutput }, CancellationToken.None);

		Assert.Equal(0, exitCode);

		string output = consoleOutput.ToString();
		Assert.Contains("Usage:", output);
		Assert.Contains("Commands:", output);
		Assert.Contains("backup2", output);
		Assert.Contains("backupTest", output);
		Assert.Contains("verify", output);
		Assert.Contains("backup4", output);
		Assert.Contains("list-sources", output);
		Assert.Contains("init-config", output);
	}

	[Theory]
	[InlineData("-r false --source-type FileSystem --source-path src --output out")]
	[InlineData("--recursive=false --source-type FileSystem --source-path src --output out")]
	public void RecursiveFlag_CanBeTurnedOffWithExplicitValue(string args)
	{
		BackupConsoleCommand4 command = new() { ServiceProvider = EmptyServiceProvider };
		ParseResult parseResult = command.Parse(args);

		Assert.Empty(parseResult.Errors);

		BackupOptionsModel4 model = new();
		model.ApplyOptions(parseResult);

		Assert.False(model.Recursive);
	}
}
