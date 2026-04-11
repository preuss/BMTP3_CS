using System.Diagnostics.CodeAnalysis;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

public interface IParser<T>
{
	bool TryParse(string? raw, [NotNullWhen(true)] out T result);
	T Parse(string raw);
	bool IsMatch(string raw);
}