using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.parsers;

public interface IParser<T>
{
	bool TryParse(string? raw, [NotNullWhen(true)] out T result);
	T Parse(string raw);
	bool IsMatch(string raw);
}
