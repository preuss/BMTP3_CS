using BMTP3.Core2.BackupNew.Api.UI;
using Spectre.Console;

namespace BMTP3.Consoles.UI;

public class SpectreConsoleNotifier : IUserNotifier
{
    private readonly IAnsiConsole _console;
    public SpectreConsoleNotifier(IAnsiConsole console) => _console = console;

    public void Info(string message) => _console.MarkupLine($"[green]{message}[/]");
    public void Warning(string message) => _console.MarkupLine($"[yellow]{message}[/]");
    public void Error(string message) => _console.MarkupLine($"[red]{message}[/]");
}
