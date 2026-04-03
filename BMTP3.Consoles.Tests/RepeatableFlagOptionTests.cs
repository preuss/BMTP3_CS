using System.CommandLine;
using BMTP3.Consoles.ConsoleCommands;

namespace BMTP3.Consoles.Tests
{
    /// <summary>
    /// Tests for <see cref="RepeatableFlagOption"/>.
    ///
    /// RepeatableFlagOption extends Option&lt;int&gt; with Arity = Zero and a CustomParser
    /// that returns the number of times the flag identifier was present on the command line
    /// (IdentifierTokenCount).  Testing is done end-to-end via System.CommandLine parsing.
    /// </summary>
    public class RepeatableFlagOptionTests
    {
        private static (Command command, RepeatableFlagOption option) BuildCommand()
        {
            var option = new RepeatableFlagOption("--verbose", "-v")
            {
                AllowMultipleArgumentsPerToken = true,
            };
            var command = new Command("test");
            command.Options.Add(option);
            return (command, option);
        }

        [Fact]
        public void ZeroOccurrences_Returns0()
        {
            var (command, option) = BuildCommand();
            var parseResult = command.Parse(Array.Empty<string>());

            int count = parseResult.GetValue(option);

            Assert.Equal(0, count);
        }

        [Fact]
        public void SingleOccurrence_ReturnsCountOf1()
        {
            var (command, option) = BuildCommand();
            var parseResult = command.Parse(["--verbose"]);

            int count = parseResult.GetValue(option);

            Assert.Equal(1, count);
        }

        [Fact]
        public void MultipleOccurrences_ReturnsCorrectCount()
        {
            var (command, option) = BuildCommand();
            var parseResult = command.Parse(["-v", "-v", "-v"]);

            int count = parseResult.GetValue(option);

            Assert.Equal(3, count);
        }
    }
}
