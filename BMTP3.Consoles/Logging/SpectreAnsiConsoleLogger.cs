using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BMTP3.Consoles.Logging;

internal class SpectreAnsiConsoleLogger : ILogger
{
	private readonly string _category;
	private readonly IAnsiConsole _console;

	public SpectreAnsiConsoleLogger(IAnsiConsole console, string category)
	{
		_console = console;
		_category = category;
	}

	public IDisposable BeginScope<TState>(TState state) where TState : notnull
	{
		return NullScope.Instance;
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return logLevel != LogLevel.None;
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
		{
			return;
		}

		string message = formatter(state, exception);

		string text = $"[{logLevel}] {_category}: {message}";

		switch (logLevel)
		{
		case LogLevel.Critical:
			_console.MarkupLine($"[bold red]{text.EscapeMarkup()}[/] ");
			break;
		case LogLevel.Error:
			_console.MarkupLine($"[red]{text.EscapeMarkup()}[/] ");
			break;
		case LogLevel.Warning:
			_console.MarkupLine($"[yellow]{text.EscapeMarkup()}[/] ");
			break;
		case LogLevel.Information:
			_console.MarkupLine($"{text.EscapeMarkup()} ");
			break;
		case LogLevel.Debug:
			_console.MarkupLine($"[blue]{text.EscapeMarkup()}[/] ");
			break;
			case LogLevel.Trace:
			default:
				_console.WriteLine(text);
				break;
		}

		if (exception != null)
		{
			_console.WriteException(exception);
		}
	}

	private class NullScope : IDisposable
	{
		public static readonly NullScope Instance = new();

		public void Dispose()
		{
		}
	}
}