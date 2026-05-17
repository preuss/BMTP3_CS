using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.Api.Models.Enums;
/// <summary>
///     Defines the level of verification performed after a file is written to the destination.
/// </summary>
public enum PostWriteVerificationType
{
	None, // Trust the file system. Fastest.
	Hash, // Read back the destination file and compare hash with source. (Default)
	Binary // Bit-by-bit comparison. Slowest but most paranoid.
}