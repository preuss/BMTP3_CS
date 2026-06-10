using Spectre.Console;

namespace BMTP3.Consoles.Extensions;

public static class AnsiConsoleExtensions
{
	public static void AddCancelKeyPressHandler(this IAnsiConsole console, ConsoleCancelEventHandler handler) => Console.CancelKeyPress += handler;

	public static void RemoveCancelKeyPressHandler(this IAnsiConsole console, ConsoleCancelEventHandler handler) => Console.CancelKeyPress -= handler;

	public static int GetCursorTop()
	{
		return Console.CursorTop;
	}
	public static int GetWindowHeight()
	{
		return Console.WindowHeight;
	}
}
