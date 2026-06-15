using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using Spectre.Console;

namespace BMTP3.Consoles.Services;

/// <summary>
///     User-facing output for the console application.
///     All output goes through IAnsiConsole (Spectre) so colors and formatting
///     are consistent and testable. This class must NOT use Console.WriteLine directly.
/// </summary>
public class ConsolesPrinter
{
	private readonly IAnsiConsole _console;

	public ConsolesPrinter(IAnsiConsole console)
	{
		_console = console;
	}

	/// <summary>
	///     Prints the options model to the console (diagnostic/verbose header).
	/// </summary>
	public void PrintOptionsModel(params BaseOptionsModel[] optionsModels)
	{
		foreach (BaseOptionsModel optionsModel in optionsModels)
		{
			string header = $"{optionsModel.GetType().Name}:";
			_console.MarkupLine($"[bold]{header.EscapeMarkup()}[/]");
			_console.WriteLine(new string('=', header.Length));
			foreach (string line in optionsModel.GetOptionPropertyValues())
			{
				_console.WriteLine("  " + line);
			}
		}
	}

	public void PrintStatus(string message)
	{
		_console.WriteLine(message);
	}

	public void PrintProgress(BMTP3.Core2.BackupNew.Api.Progress.IBackupProgress progress)
	{
		_console.WriteLine(
			$"{progress.Phase}: discovered={progress.FilesDiscovered} succeeded={progress.FilesSucceeded} failed={progress.FilesFailed}");
	}

	public void PrintProgress(BMTP3.Core3.IBackupProgress progress)
	{
		_console.WriteLine(
			$"{progress.Phase}: file={progress.CurrentFile} processed={progress.FilesProcessed}/{progress.FilesTotal} bytes={progress.BytesTransferred}");
	}

	public void PrintProgress(BackupProgress progress)
	{
		_console.WriteLine(
			$"{progress.CurrentPhase}: discovered={progress.FilesDiscovered} succeeded={progress.FilesSucceeded} failed={progress.FilesFailed}");
	}

	public void PrintResult(BMTP3.Core2.BackupNew.Api.Response.BackupJobResult result)
	{
		_console.MarkupLine($"[bold]Job[/] '[green]{result.JobName.EscapeMarkup()}[/]' finished: {result.Status}");
		_console.WriteLine(
			$"Scanned: {result.TotalFilesScanned} Copied: {result.FilesCopied} Failed: {result.FilesFailed} Skipped: {result.FilesSkipped} Bytes: {result.TotalBytesCopied}");
		if (result.GlobalErrors?.Count > 0)
		{
			_console.MarkupLine("[yellow]Global errors:[/]");
			foreach (string e in result.GlobalErrors)
			{
				_console.MarkupLine($"  [yellow]- {e.EscapeMarkup()}[/]");
			}
		}
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

	public void PrintError(string message)
	{
		_console.MarkupLine($"[red]Error: {message.EscapeMarkup()}[/]");
	}
}