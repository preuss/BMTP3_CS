using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.ParserElements;
/// <summary>Defines the strategy for creating the destination folder structure.</summary>
public enum OutputStructureStrategies {
	PreserveSourceTree, // Preserves the original folder hierarchy from the source.
	Flat,               // Places all files directly into the root destination folder.
	CustomFilePath,     // Uses the CustomOutputFilePath to define the entire path, including folders and filename.
}