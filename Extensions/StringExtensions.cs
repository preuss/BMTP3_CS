using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Extensions {
	public static class StringExtensions {
		public static string? RemoveNewLines(this string? text) {
			return text?.ReplaceExact("\r\n", string.Empty)
				?.ReplaceExact("\n", string.Empty);
		}

		internal static string ReplaceExact(this string text, string oldValue, string? newValue) {
#if NETSTANDARD2_0
        return text.Replace(oldValue, newValue);
#else
			return text.Replace(oldValue, newValue, StringComparison.Ordinal);
#endif
		}
	}
}
