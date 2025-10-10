using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ConsoleCommands;

public class BackupConsoleCommand : Command
{
    public BackupConsoleCommand() : base("backup", "Perform backup")
    {
        SetAction(HandleBackupAsync);
    }

    /// <summary>
    /// Entry point for the backup command action.
    /// </summary>
    private async Task<int> HandleBackupAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
	    int verbosity = 0;
		if(parseResult.GetResult("--verbose") is OptionResult verboseOpt)
		{
			verbosity = verboseOpt.IdentifierTokenCount;
		}

	    Console.WriteLine("Verbosity Level: " + verbosity);

		Console.WriteLine($"Verbosity Level: {verbosity}");
		// Call private methods to structure your code
		await DoBackupAsync(cancellationToken);
        PrintResult();
        return 0;
    }

    /// <summary>
    /// Simulates backup work. Replace with actual backup logic.
    /// </summary>
    private async Task DoBackupAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
        // Add your actual backup logic here
    }

    /// <summary>
    /// Prints the result of the backup operation.
    /// </summary>
    private void PrintResult()
    {
        Console.WriteLine("Backup completed!");
    }
}

