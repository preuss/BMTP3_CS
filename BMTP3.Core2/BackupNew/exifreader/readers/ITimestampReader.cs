using BMTP3.Core2.BackupNew.candidates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.exifreader.readers;
public interface ITimestampReader
{
	/// <summary>
	/// Reads timestamp candidates from the given file.
	/// Implementations must be side-effect free and must not throw on malformed metadata.
	/// </summary>
	IReadOnlyList<TimestampCandidate> Read(FileInfo file);
}