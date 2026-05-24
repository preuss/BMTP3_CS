using System.Diagnostics.CodeAnalysis;

namespace BMTP3.Core4.Engine.TimeStamp.Parsers;

public abstract class ParserBase<T> : IParser<T>
{
	public abstract bool TryParse(string? raw, [NotNullWhen(true)] out T result);

	public virtual T Parse(string raw)
	{
		if (TryParse(raw, out T? result) && result != null)
		{
			return result;
		}

		throw new FormatException($"Invalid {typeof(T).Name.ToLower()}: '{raw}'");
	}

	public virtual bool IsMatch(string raw)
	{
		return TryParse(raw, out _);
	}

	public virtual T? ParseOrNull(string? raw)
	{
		return TryParse(raw, out T? result) ? result : default;
	}
}