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
			if (!Options.Contains(option))
			{
				Options.Add(option);
			}
			else
			{
				throw new InvalidOperationException($"Option {option.Name} is already defined.");
			}
		}

		foreach(Option option in new TOptionsModel().GetAllOptions())
		{
			if (!Options.Contains(option))
			{
				Options.Add(option);
			}
			else
			{
				throw new InvalidOperationException($"Option {option.Name} is already defined.");
			}
		}

		SetAction(HandleAsyncInternal);
	}
	private async Task<int> HandleAsyncInternal(ParseResult parseResult, CancellationToken cancellationToken)
	{
		try
		{
			TGlobalOptionsModel globalOptionsModel = DoBindGlobalOptionsModel(parseResult);
			TOptionsModel optionsModel = DoBindOptionsModel(parseResult);
			return await DoCommandAsync(globalOptionsModel, optionsModel, parseResult, cancellationToken);
		}
		catch (Exception ex)
		{
			await Console.Error.WriteLineAsync($"Error executing command: {ex.Message}");
			return 1;
		}
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
