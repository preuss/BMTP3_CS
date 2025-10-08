using System;
using System.CommandLine;
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
	    var verboseOpt = this.Options
		    .OfType<Option<int>>()
		    .FirstOrDefault(o =>
			    o.Aliases.Contains("-v") ||
			    o.Aliases.Contains("--verbose") ||
			    string.Equals(o.Name, "verbose", StringComparison.OrdinalIgnoreCase));

	    int verbosity = 0;
		/*
	    if(verboseOpt != null)
	    {
		    // 2) Hent OptionResult via ParseResult.FindResultFor
		    var optResult = parseResult.FindResultFor(verboseOpt);

		    if(optResult != null)
		    {
			    // 3) Foretrukket: få den typede værdi som CustomParser returnerede
			    //    Hvis GetValueOrDefault<T>() ikke findes i netop din build, fallback til Tokens.Count
			    try
			    {
				    verbosity = optResult.GetValueOrDefault<int>();
			    } catch(MissingMethodException)
			    {
				    verbosity = optResult.Tokens.Count;
			    }
			    // fallback sikkerhed:
			    if(verbosity == 0 && optResult.Tokens != null)
				    verbosity = optResult.Tokens.Count;
		    }
	    }*/

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

