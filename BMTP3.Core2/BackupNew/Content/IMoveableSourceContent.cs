using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Content;
public interface IMoveableSourceContent : ISourceContent
{
	/// <summary>
	/// Moves the physical file to a new location and returns a new ISourceContent representing that location.
	/// This is an atomic operation on the file system.
	/// </summary>
	ISourceContent MoveTo(string destinationPath);
}
