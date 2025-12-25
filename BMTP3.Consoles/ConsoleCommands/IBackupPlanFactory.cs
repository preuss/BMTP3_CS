using BMTP3.Core2.BackupNew.Api.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BMTP3.Consoles.ConsoleCommands;
/// <summary>
/// Contract to map Console option model into a validated <see cref="BackupPlan"/>.
/// Implemented by Console layer or an adapter in the composition root.
/// </summary>
public interface IBackupPlanFactory
{
	/// <summary>Maps and validates options; returns validation errors (empty = valid).</summary>
	BackupPlan? CreateAndValidate(BackupOptionsModel options, out IReadOnlyList<string> Errors);
}
