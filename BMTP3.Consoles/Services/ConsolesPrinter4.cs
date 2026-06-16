using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using Spectre.Console;

namespace BMTP3.Consoles.Services;

/// <summary>
///     Console output for Core4 operations. Shared methods are inherited from <see cref="ConsolesPrinter"/>.
///     All output goes through IAnsiConsole (Spectre) so colors and formatting
///     are consistent and testable. This class must NOT use Console.WriteLine directly.
/// </summary>
public class ConsolesPrinter4 : ConsolesPrinter
{
	public ConsolesPrinter4(IAnsiConsole console) : base(console)
	{
	}

	public void PrintProgress(BackupProgress progress)
	{
		_console.WriteLine(
			$"{progress.CurrentPhase}: discovered={progress.FilesDiscovered} succeeded={progress.FilesSucceeded} failed={progress.FilesFailed}");
	}

	public void PrintResult(BackupResult result)
	{
		string stateColor = result.State switch
		{
			BackupResultState.Completed => "green",
			BackupResultState.Cancelled => "yellow",
			BackupResultState.Failed => "red",
			_ => "white",
		};
		_console.MarkupLine($"[bold]Job[/] '[green]{result.Name.EscapeMarkup()}[/]' finished: [{stateColor}]{result.State}[/]");
		BackupResultCounts counts = result.Counts;
		_console.WriteLine($"Total: {counts.Total} Succeeded: {counts.Succeeded} Failed: {counts.Failed} Skipped: {counts.Skipped}");
		if (result.FailureReason is not null)
		{
			_console.MarkupLine($"[yellow]Failure reason: {result.FailureReason.ToString().EscapeMarkup()}[/]");
		}
	}
}
