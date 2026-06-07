using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.Devices;
/// <summary>
/// Provides attributes for files, directories and objects.
/// </summary>
[Flags]
internal enum MediaFileAttribute
{
	/// <summary>The file is a standard file.</summary>
	Normal = 1,
	/// <summary>The file is a directory.</summary>
	Directory = 2,
	/// <summary>The file is an object.</summary>
	Object = 4,
	/// <summary>This file can be deleted.</summary>
	CanDelete = 16, // 0x00000010
	/// <summary>
	/// The file is a system file. That is, the file is part of the operating system or is used exclusively by the operating system.
	/// </summary>
	System = 32, // 0x00000020
	/// <summary>
	/// The file is hidden, and thus is not included in an ordinary directory listing.
	/// </summary>
	Hidden = 64, // 0x00000040
	/// <summary>The file is DRM protected.</summary>
	DRMProtected = 128, // 0x00000080
}
