using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.parsers;
public abstract class ParserBase<T> : IParser<T>
{
	public abstract bool TryParse(string? raw, [NotNullWhen(true)] out T result);

	public virtual T Parse(string raw)
	{
		if(TryParse(raw, out T? result) && result != null)
		{
			return result;
		}
		throw new FormatException($"Invalid {typeof(T).Name.ToLower()}: '{raw}'");
	}

	public virtual T? ParseOrNull(string? raw)
	{
		return TryParse(raw, out T? result) ? result : default;
	}

	public virtual bool IsMatch(string raw)
	{
		return TryParse(raw, out _);
	}
}
