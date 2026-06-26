using System.Text;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.Tests.Utils;

// Simple in-memory logger for tests. Thread-safe for append-only use.
public class TestLogger<T> : ILogger<T>, IDisposable
{
	private readonly StringBuilder _sb = new();

	public string Logs => _sb.ToString();

	void IDisposable.Dispose()
	{
	}

	IDisposable ILogger.BeginScope<TState>(TState state)
	{
		return this;
	}

	public bool IsEnabled(LogLevel logLevel)
	{
		return true;
	}

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		string line = $"[{DateTime.UtcNow:O}] {logLevel}: {formatter(state, exception)}";
		if (exception != null)
		{
			line += $" Exception: {exception.GetType().Name} {exception.Message}";
		}

		lock (_sb)
		{
			_sb.AppendLine(line);
		}
	}
}