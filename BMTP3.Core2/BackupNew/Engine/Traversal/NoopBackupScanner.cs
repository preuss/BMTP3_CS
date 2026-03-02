using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;
/// <summary>
/// No-op implementation of IBackupScanner that returns no items.
/// Use as a safe default so library users can override IBackupScanner in DI.
/// </summary>
public sealed class NoopBackupScanner : IBackupScanner
{
	public async IAsyncEnumerable<IBackupItem> ScanAsync(BackupPlan job, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
	{
		await Task.CompletedTask;
		yield break;
	}
}
