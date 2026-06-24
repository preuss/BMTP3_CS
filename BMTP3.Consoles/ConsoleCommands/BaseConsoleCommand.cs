using System.CommandLine;
using System.CommandLine.Parsing;

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

		// Register all options exposed by the supplied option models on this command.
		foreach(BaseOptionsModel optionsModel in _optionsModels)
		{
			// Each model owns its option definitions; the command only attaches them.
			foreach(Option option in optionsModel.GetAllOptions())
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

		// Validate that no option name or alias is reused across models.
		BaseOptionsModel.ValidateDuplicateOptionNamesOrAliases(_optionsModels);

		// Register command-level validation with System.CommandLine.
		// This runs before ExecuteInternalAsync and can stop command execution
		// when the command input is invalid as a whole.
		Validators.Add(result =>
		{
			ValidateCommandOptions(result);
		});

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

	/// <summary>
	///     Binds parsed command-line values to the provided options model.
	/// </summary>
	protected virtual BaseOptionsModel DoBindOptionsModel(ParseResult parseResult, BaseOptionsModel optionsModel)
	{
		optionsModel.ApplyOptions(parseResult);
		return optionsModel;
	}

	/// <summary>
	///     Runs command-level validation for this command.
	/// </summary>
	/// <remarks>
	///     This method is called by System.CommandLine before command execution.
	///     Override DoValidateCommandOptions in derived commands to add validation rules
	///     that depend on the command input as a whole.
	/// </remarks>
	public void ValidateCommandOptions(CommandResult result)
	{
		ArgumentNullException.ThrowIfNull(result);
		DoValidateCommandOptions(result);
	}

	/// <summary>
	///     Override to add command-level validation rules.
	/// </summary>
	/// <remarks>
	///     Use this for validation that depends on the command input as a whole,
	///     including missing options or combinations of multiple options.
	///
	///     Option model properties are not populated yet at this point.
	///     Use result.GetResult(SomeOption) to inspect whether an option was supplied,
	///     and call result.AddError(...) to report validation errors.
	/// </remarks>
	protected virtual void DoValidateCommandOptions(CommandResult result)
	{
	}

	/// <summary>
	///     Main implementation point for derived commands.
	///     Called after parsing, validation, and option binding have completed.
	/// </summary>
	/// <returns>
	///     The command exit code.
	/// </returns>
	protected abstract Task<int> DoExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken);
}