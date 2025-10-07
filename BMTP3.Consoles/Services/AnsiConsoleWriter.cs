using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Services;
internal class AnsiConsoleWriter : IConsoleWriter {
	public void WriteLine(string? text = null) {
		AnsiConsole.WriteLine(text ?? "");
	}

	public void Write(string? text = null) {
		if(text != null) {
			AnsiConsole.Write(text);
		}
	}
}

