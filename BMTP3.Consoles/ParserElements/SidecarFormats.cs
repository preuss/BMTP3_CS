using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ParserElements;
/// <summary>Defines the type of metadata file (sidecar) to be created next to each backed-up file.</summary>
public enum SidecarFormats {
	None,       // No metadata file is created per file.
	Ini,        // Creates an INI file (e.g., file.jpg.ini) next to each file.
	Json,       // Creates a JSON file (e.g., file.jpg.json) next to each file.
}
