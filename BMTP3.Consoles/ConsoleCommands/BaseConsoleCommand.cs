using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;
public abstract class BaseConsoleCommand : Command
{
	private readonly BaseOptionsModel[] _optionsModels;
	protected BaseConsoleCommand(
		string name,
		string? description = null,
		params BaseOptionsModel[] optionsModels
	) : base(name, description)
	{
		_optionsModels = optionsModels;
		foreach(BaseOptionsModel optionsModel in _optionsModels)
		{
			foreach(Option option in optionsModel.GetAllOptions())
			{
				if(!Options.Contains(option))
				{
					Options.Add(option);
				} else
				{
					throw new InvalidOperationException($"Option {option.Name} is already defined.");
				}
			}
		}
		BaseOptionsModel.ValidateDuplicateNameAndAlias(_optionsModels);

		SetAction(ExecuteInternalAsync);
	}
	private async Task<int> ExecuteInternalAsync(ParseResult parseResult, CancellationToken cancellationToken)
	{
		try
		{
			foreach(var optionsModel in _optionsModels)
			{
				DoBindOptionsModel(parseResult, optionsModel);
			}
			return await DoExecuteAsync(parseResult, cancellationToken);
		} catch(Exception ex)
		{
			await Console.Error.WriteLineAsync($"Error executing command: {ex.Message}");
			return 1;
		}
	}
	protected virtual BaseOptionsModel DoBindOptionsModel(ParseResult parseResult, BaseOptionsModel optionsModel)
	{
		optionsModel.ApplyOptions(parseResult);
		return optionsModel;
	}
	protected abstract Task<int> DoExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
