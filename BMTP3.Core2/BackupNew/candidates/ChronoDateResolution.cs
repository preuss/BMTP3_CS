using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.candidates;

public enum ChronoDateResolution
{
	/// <summary>
	/// Date (year, month, day).
	/// </summary>
	FullDate,
	/// <summary>
	/// Year and Month only (year, month).
	/// </summary>
	YearAndMonth,
	/// <summary>
	/// Year only (year).
	/// </summary>
	YearOnly,
}
