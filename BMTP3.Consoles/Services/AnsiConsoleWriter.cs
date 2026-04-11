using Spectre.Console;

namespace BMTP3.Consoles.Services;

internal class AnsiConsoleWriter : IConsoleWriter
{
	public void WriteLine(string? text = null)
	{
		AnsiConsole.WriteLine(text ?? "");
	}

	public void Write(string? text = null)
	{
		if (text != null)
		{
			AnsiConsole.Write(text);
		}
	}
}