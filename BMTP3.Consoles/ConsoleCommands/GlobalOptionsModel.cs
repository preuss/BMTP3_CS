namespace BMTP3.Consoles.ConsoleCommands;

public class GlobalOptionsModel : BaseOptionsModel
{
	/*
	public static Option<int> VerboseOption { get; } = new("--verbose", "-v")
	{
		Description = "Enable verbose output. Repeat for more detail.",
		Arity = ArgumentArity.Zero,
		CustomParser = result => result.GetResult(VerboseOption)?.IdentifierTokenCount ?? 0
	};
	*/
	public static RepeatableFlagOption VerboseOption { get; } = new("--verbose", "-v")
	{
		Description = "Enable verbose output. Repeat for more detail.",
	};

	public int Verbose { get; set; } = 0;
}