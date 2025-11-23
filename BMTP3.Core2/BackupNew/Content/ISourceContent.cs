using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.BackupNew.Content;
/// <summary>
/// Minimal abstraction over any source of file content.
/// Provides only the size and a readable stream – no metadata, no path information.
/// </summary>
public interface ISourceContent : IDisposable {
	/// <summary>
	/// Size of the content in bytes. Used for progress reporting and buffer allocation.
	/// </summary>
	ulong Length { get; }

	/// <summary>
	/// Opens a new readable stream to the content.
	/// The caller is responsible for disposing the returned stream.
	/// </summary>
	Stream OpenRead();
}