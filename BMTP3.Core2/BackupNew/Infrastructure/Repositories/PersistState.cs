using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Infrastructure.Repositories;
/// <summary>
/// Persistence status for resume decisions.
/// </summary>
public enum PersistState
{
	Pending = 0,    // Item not yet persisted
	Completed = 10, // Item persisted successfully
	Skipped = 20    // Item intentionally not persisted
}