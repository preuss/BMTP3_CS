using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Consoles.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace BMTP3.Consoles;

public class Program {
	public static async Task<int> Main(string[] args) {
		IServiceProvider serviceProvider = ApplicationStartup.CreateConfiguration(args);

		var app = serviceProvider.GetRequiredService<ConsoleApplication>();

		var rootCommand = new RootCommand("BMTP3 CLI");
		rootCommand.Add(new BackupConsoleCommand());
		rootCommand.Add(new VerifyConsoleCommand());

		var parseResult= rootCommand.Parse(args);
		await parseResult.InvokeAsync();
		//await app.RunAsync();
		await Task.CompletedTask;
		return 0;
	}
}
