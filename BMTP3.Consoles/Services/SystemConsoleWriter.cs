using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Services;
internal class SystemConsoleWriter : IConsoleWriter {
	public void WriteLine(string? text = null) {
		System.Console.WriteLine(text);
	}

	public void Write(string? text = null) {
		System.Console.Write(text);
	}
}
