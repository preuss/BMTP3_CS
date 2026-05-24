using System.Diagnostics.CodeAnalysis;

namespace BMTP3.Core4.Engine.TimeStamp.Parsers;

public interface IParser<T>
{
	bool TryParse(string? raw, [NotNullWhen(true)] out T result);
	T Parse(string raw);
	bool IsMatch(string raw);
}