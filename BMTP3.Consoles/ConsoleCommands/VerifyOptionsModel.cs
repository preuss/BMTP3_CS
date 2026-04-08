using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class VerifyOptionsModel : BaseOptionsModel
{
	public static Option<bool> DeepValidationOption { get; } = new("--deep-validation", "-d");

	public bool DeepValidation { get; set; }
	/*
	protected override Dictionary<Option, Action<ParseResult>> DoDefineOptions() {
		OptionsBuilder optionsBuilder = new(this);
		optionsBuilder.AddOption(DeepValidation,
			new Option<bool>("--deep-validation", "-d")
		);
		return optionsBuilder.Build();
	}
	*/
}