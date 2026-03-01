using BMTP3.Consoles.ConsoleCommands;
using BMTP3.Core2.BackupNew.Api.Progress;
using BMTP3.Core2.BackupNew.Api.Response;

namespace BMTP3.Consoles.Services;
public class ConsolesPrinter
{
	/// <summary>
	/// Prints the options model to the console.
	/// </summary>
	/// <param name="optionsModels">The options models to print.</param>
	public void PrintOptionsModel(params BaseOptionsModel[] optionsModels)
	{
		foreach(BaseOptionsModel optionsModel in optionsModels)
		{
			string header = $"{optionsModel.GetType().Name}:";
			Console.WriteLine(header);
			Console.WriteLine(new string('=', header.Length));
			foreach(string line in optionsModel.GetOptionPropertyValues())
			{
				Console.WriteLine("  " + line);
			}
		}
	}

	public void PrintStatus(string message)
	{
		Console.WriteLine(message);
	}

	public void PrintProgress(IBackupProgress progress)
	{
		// Simple progress line for user consumption
		Console.WriteLine($"{progress.Phase}: discovered={progress.FilesDiscovered} succeeded={progress.FilesSucceeded} failed={progress.FilesFailed}");
	}

	public void PrintResult(BackupJobResult result)
	{
		Console.WriteLine($"Job '{result.JobName}' finished: {result.Status}");
		Console.WriteLine($"Scanned: {result.TotalFilesScanned} Copied: {result.FilesCopied} Failed: {result.FilesFailed} Skipped: {result.FilesSkipped} Bytes: {result.TotalBytesCopied}");
		if(result.GlobalErrors?.Count > 0)
		{
			Console.WriteLine("Global errors:");
			foreach(string e in result.GlobalErrors) Console.WriteLine($"  - {e}");
		}
	}

	public void PrintError(string message)
	{
		Console.Error.WriteLine(message);
	}
}
