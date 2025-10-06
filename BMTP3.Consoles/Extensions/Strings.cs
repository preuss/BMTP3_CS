using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Extensions;
public static class Strings {
	public static string? ToNullIfNullOrWhiteSpace(this string? str) {
		if(String.IsNullOrWhiteSpace(str)) {
			return null;
		} else {
			return str;
		}
	}
}
