using System.CommandLine;
using BMTP3.Consoles.ConsoleCommands;

namespace BMTP3.Consoles.Tests;

/// <summary>
///     Tests for <see cref="RepeatableFlagOption" />.
///     RepeatableFlagOption extends Option&lt;int&gt; with Arity = Zero and a CustomParser
///     that returns the number of times the flag identifier was present on the command line
///     (IdentifierTokenCount).  Testing is done end-to-end via System.CommandLine parsing.
/// </summary>
public class RepeatableFlagOptionTests
{
	private static (Command command, RepeatableFlagOption option) BuildCommand()
	{
		RepeatableFlagOption option = new("--verbose", "-v")
		{
			AllowMultipleArgumentsPerToken = true
		};
		Command command = new("test");
		command.Options.Add(option);
		return (command, option);
	}

	[Fact]
	public void ZeroOccurrences_Returns0()
	{
		(Command command, RepeatableFlagOption option) = BuildCommand();
		ParseResult parseResult = command.Parse(Array.Empty<string>());

		int count = parseResult.GetValue(option);

		Assert.Equal(0, count);
	}

	[Fact]
	public void SingleOccurrence_ReturnsCountOf1()
	{
		(Command command, RepeatableFlagOption option) = BuildCommand();
		ParseResult parseResult = command.Parse(["--verbose"]);

		int count = parseResult.GetValue(option);

		Assert.Equal(1, count);
	}

	[Fact]
	public void MultipleOccurrences_ReturnsCorrectCount()
	{
		(Command command, RepeatableFlagOption option) = BuildCommand();
		ParseResult parseResult = command.Parse(["-v", "-v", "-v"]);

		int count = parseResult.GetValue(option);

		Assert.Equal(3, count);
	}
}