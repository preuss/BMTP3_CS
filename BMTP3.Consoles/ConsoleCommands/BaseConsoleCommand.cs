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
		foreach (BaseOptionsModel optionsModel in _optionsModels)
		{
			foreach (Option option in optionsModel.GetAllOptions())
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
		}

		BaseOptionsModel.ValidateDuplicateNameAndAlias(_optionsModels);

		SetAction(ExecuteInternalAsync);
	}

	private async Task<int> ExecuteInternalAsync(ParseResult parseResult, CancellationToken cancellationToken)
	{
		try
		{
			foreach (BaseOptionsModel optionsModel in _optionsModels)
			{
				DoBindOptionsModel(parseResult, optionsModel);
			}

			return await DoExecuteAsync(parseResult, cancellationToken);
		}
		catch (Exception ex)
		{
			OnCommandError(ex);
			return 1;
		}
	}

	/// <summary>
	///     Called when an unhandled exception occurs during command execution.
	///     Override in subclasses to route errors through a printer or notifier.
	///     Default falls back to stderr.
	/// </summary>
	protected virtual void OnCommandError(Exception ex)
	{
		Console.Error.WriteLine($"Error executing command: {ex.Message}");
	}

	protected virtual BaseOptionsModel DoBindOptionsModel(ParseResult parseResult, BaseOptionsModel optionsModel)
	{
		optionsModel.ApplyOptions(parseResult);
		return optionsModel;
	}

	protected abstract Task<int> DoExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);
}