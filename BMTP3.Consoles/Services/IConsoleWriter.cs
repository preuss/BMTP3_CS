using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.Services;
public interface IConsoleWriter {
	void WriteLine(string? text = null);
	void Write(string? text = null);
}

