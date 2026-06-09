using BMTP3.Consoles.Services;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class VerifyConsoleCommand : BaseConsoleCommand
{
	public VerifyConsoleCommand() : this("verify", "Verificér backup", new GlobalOptionsModel(),
		new VerifyOptionsModel())
	{
	}

	public VerifyConsoleCommand(
		string name,
		string description,
		GlobalOptionsModel globalOptionsModel,
		VerifyOptionsModel verifyOptionsModel
	) : base(name, description, globalOptionsModel, verifyOptionsModel)
	{
		GlobalOptions = globalOptionsModel;
		VerifyOptions = verifyOptionsModel;
	}

	public GlobalOptionsModel GlobalOptions { get; }
	public VerifyOptionsModel VerifyOptions { get; }
	public IServiceProvider? ServiceProvider { get; init; }

	protected override async Task<int> DoExecuteAsync(
		ParseResult parseResult,
		CancellationToken cancellationToken
	)
	{
		ConsolesPrinter? printer = ServiceProvider?.GetService<ConsolesPrinter>();
		printer?.PrintStatus("Verify not yet implemented.");
		printer?.PrintStatus($"DeepValidation: {VerifyOptions.DeepValidation}");
		await Task.Delay(1000, cancellationToken);
		return 0;
	}
}