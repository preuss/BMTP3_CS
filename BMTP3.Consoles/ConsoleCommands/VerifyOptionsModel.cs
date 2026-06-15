using System.CommandLine;

namespace BMTP3.Consoles.ConsoleCommands;

public class VerifyOptionsModel : BaseOptionsModel
{
	public static Option<bool> DeepValidationOption { get; } = new("--deep-validation", "-d");

	public bool DeepValidation { get; set; }

}