using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Engine.State;
using BMTP3.Core4.Helpers;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Scanner;

namespace BMTP3.Core4.Engine.Sequential;

internal sealed class SequentialBackupEngine : IBackupRunner
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
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		// 1. Open backup session state
		BackupSessionStateKey sessionKey = BackupSessionStateKeyFactory.Create(plan);

		BackupSessionState session = await sessionStateStore.OpenAsync(
			sessionKey,
			cancellationToken
		);

		// 2. Scan source and populate backup session state
		session.SetPhase(BackupPhase.Scanning);

		await foreach(BackupItem item in scanner.ScanAsync(plan, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			session.AddItem(item);
		}

		throw new NotImplementedException();
	}
}
