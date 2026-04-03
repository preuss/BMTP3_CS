using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BMTP3.Consoles.Logging;

public class SpectreAnsiConsoleLoggerProvider : ILoggerProvider
{
    private readonly IAnsiConsole _console;

    public SpectreAnsiConsoleLoggerProvider(IAnsiConsole console)
    {
        _console = console;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SpectreAnsiConsoleLogger(_console, categoryName);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
