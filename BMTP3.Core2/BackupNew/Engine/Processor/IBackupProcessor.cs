using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Processor;
public interface IBackupProcessor
{
	/// <summary>
	/// Executes a backup job from start to finish.
	/// This is the entry point to the entire system.
	/// </summary>
	Task ProcessJobAsync(BackupPlan job, IProgress<BackupProgress> progress, CancellationToken ct);
}