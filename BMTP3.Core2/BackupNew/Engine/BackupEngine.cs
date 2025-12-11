using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Domain.Job;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Engine.Steps;
using BMTP3.Core2.BackupNew.Infrastructure.Repositories;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Response;
using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.BackupNew.Engine;

/// <summary>
/// The main engine that orchestrates the backup process.
/// </summary>
public class BackupEngine : IBackupEngine
{
	public Task<BackupJobResult> RunAsync(BackupPlan job, IProgress<BackupProgress> progress, CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}