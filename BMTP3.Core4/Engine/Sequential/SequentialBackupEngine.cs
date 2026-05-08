using BMTP3.Core4.Api;
using BMTP3.Core4.Engine.State;
using BMTP3.Core4.Engine.Validation;
using BMTP3.Core4.Helpers;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Scanner;

namespace BMTP3.Core4.Engine.Sequential;

internal sealed class SequentialBackupEngine : IBackupEngine
{
	private readonly IBackupScanner scanner;
	private readonly IBackupSessionStateStore sessionStateStore;

	public SequentialBackupEngine(
		IBackupScanner scanner,
		IBackupSessionStateStore sessionStateStore)
	{
		this.scanner = Guard.RequireNonNull(scanner);
		this.sessionStateStore = Guard.RequireNonNull(sessionStateStore);
	}

	public async Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<IBackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		// 1. Validate backup plan
		BackupPlanValidator.Validate(plan);

		// 2. Open backup session state
		BackupSessionStateKey sessionKey = BackupSessionStateKeyFactory.Create(plan);

		BackupSessionState session = await sessionStateStore.OpenAsync(
			sessionKey,
			cancellationToken
		);

		// 3. Scan source and populate backup session state
		session.SetPhase(BackupPhase.Scanning);

		await foreach(BackupItem item in scanner.ScanAsync(plan, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			session.AddItem(item);
		}

		throw new NotImplementedException();
	}
}