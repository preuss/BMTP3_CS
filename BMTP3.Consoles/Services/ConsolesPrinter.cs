using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Response;
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
			_console.MarkupLine($"[bold]{header}[/]");
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

	public void PrintProgress(IBackupProgress progress)
	{
		_console.WriteLine(
			$"{progress.Phase}: discovered={progress.FilesDiscovered} succeeded={progress.FilesSucceeded} failed={progress.FilesFailed}");
	}

	public void PrintResult(BackupJobResult result)
	{
		_console.MarkupLine($"[bold]Job '[green]{result.JobName}[/]' finished: {result.Status}[/]");
		_console.WriteLine(
			$"Scanned: {result.TotalFilesScanned} Copied: {result.FilesCopied} Failed: {result.FilesFailed} Skipped: {result.FilesSkipped} Bytes: {result.TotalBytesCopied}");
		if (result.GlobalErrors?.Count > 0)
		{
			_console.MarkupLine("[yellow]Global errors:[/]");
			foreach (string e in result.GlobalErrors)
			{
				_console.MarkupLine($"  [yellow]- {e}[/]");
			}
		}
	}

	public void PrintError(string message)
	{
		_console.MarkupLine($"[red]Error: {message}[/]");
	}
}