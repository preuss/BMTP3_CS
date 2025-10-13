using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;
public abstract class BaseConsoleCommand<TGlobalOptionsModel, TOptionsModel> : Command
	where TGlobalOptionsModel : BaseOptionsModel, new()
	where TOptionsModel : BaseOptionsModel, new()
{
	protected BaseConsoleCommand(string name, string? description = null) : base(name, description)
	{
		foreach(Option option in new TGlobalOptionsModel().GetAllOptions())
		{
			Options.Add(option);
		}

		foreach(Option option in new TOptionsModel().GetAllOptions())
		{
			Options.Add(option);
		}

		SetAction(HandleAsyncInternal);
	}
	private async Task<int> HandleAsyncInternal(ParseResult parseResult, CancellationToken cancellationToken)
	{
		TGlobalOptionsModel globalOptionsModel = DoBindGlobalOptionsModel(parseResult);
		TOptionsModel optionsModel = DoBindOptionsModel(parseResult);
		return await DoCommandAsync(globalOptionsModel, optionsModel, parseResult, cancellationToken);
	}

	protected virtual TGlobalOptionsModel DoBindGlobalOptionsModel(ParseResult parseResult)
	{
		TGlobalOptionsModel globalOptionsModel = new();
		globalOptionsModel.PopulateFromParseResult(parseResult);

		return globalOptionsModel;
	}

	protected virtual TOptionsModel DoBindOptionsModel(ParseResult parseResult)
	{
		TOptionsModel optionsModel = new();
		optionsModel.PopulateFromParseResult(parseResult);
		return optionsModel;
	}
	protected abstract Task<int> DoCommandAsync(TGlobalOptionsModel globalOptionsModel, TOptionsModel optionsModel, ParseResult parseResult, CancellationToken cancellationToken);
}
