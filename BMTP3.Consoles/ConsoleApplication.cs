using BMTP3.Consoles.Services;

namespace BMTP3.Consoles;
public class ConsoleApplication
{
	private readonly IClock _clock;
	private readonly IConsoleWriter _writer;

	public ConsoleApplication(IClock clock, IConsoleWriter writer)
	{
		_clock = clock;
		_writer = writer;
	}

	public async Task<int> RunAsync(string[] args)
	{
		_writer.WriteLine($"Programmet startede: {_clock.UtcNow}");
		_writer.WriteLine("Argumenter modtaget:");
		foreach(string arg in args)
		{
			_writer.WriteLine(arg);
		}

		// Her kan du udvide med command parsing, service calls osv.
		await Task.Delay(100); // Simulerer async arbejde

		return 0; // Exit code
	}

}