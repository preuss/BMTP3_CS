using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Domain.Item;
public enum LifecycleState
{
	/// <summary>
	/// Item has been created in the system.
	/// </summary>
	New,

	/// <summary>
	/// Item is queued and waiting to be processed.
	/// </summary>
	Queued,

	/// <summary>
	/// Item is active and being handled by the backup process.
	/// </summary>
	Active,

	/// <summary>
	/// Item has been processed and has a final ResultState.
	/// </summary>
	Processed
}

