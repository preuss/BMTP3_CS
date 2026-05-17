using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.State;
using BMTP3.Core4.Engine.Validation;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Scanner;

namespace BMTP3.Core4.Engine;

/// <summary>
/// Orchestrates the execution of a backup job.
/// This implementation defines the ordered steps of a backup run.
/// Each step is initially expressed as a comment and will later
/// be replaced by concrete implementation calls.
/// </summary>
public sealed class BackupEngine : IBackupEngine
{
	private readonly IBackupScanner _scanner;
	private readonly IBackupSessionStateStore _sessionStateStore;

	internal BackupEngine(
		IBackupScanner scanner,
		IBackupSessionStateStore sessionStateStore)
	{
		_scanner = scanner;
		_sessionStateStore = sessionStateStore;
	}

	public async Task<BackupResult> RunAsync(
		BackupPlan plan,
		IProgress<BackupProgress>? progress,
		CancellationToken cancellationToken)
	{
		// ------------------------------------------------------------
		// 1. Validate backup plan
		//    - Ensure required fields are present
		//    - Ensure source and destination are not the same
		//    - Fail fast on invalid configuration
		// ------------------------------------------------------------
		BackupPlanValidator.Validate(plan);

		// ------------------------------------------------------------
		// 2. Initialize backup state
		//    - Create internal state objects
		//    - Initialize progress tracking
		//    - Set phase = Starting
		// ------------------------------------------------------------
		BackupSessionStateKey sessionKey = BackupSessionStateKeyFactory.Create(plan);

		BackupSessionState session = await _sessionStateStore.OpenAsync(
			sessionKey,
			cancellationToken
		);

		session.SetPhase(BackupPhase.Scanning);

		BackupScanRequest scanRequest = new(
			plan.SourcePath,
			plan.Recursive,
			plan.IncludePatterns,
			plan.ExcludePatterns);

		Progress<BackupScanProgress> scanProgress = new(sp =>
		{
			progress?.Report(new()
			{
				SourcePath = plan.SourcePath,
				DestinationPath = plan.Destination,
				CurrentPhase = BackupProgressPhase.Scanning,
				DirectoriesTraversed = sp.DirectoriesTraversed,
				FilesDiscovered = sp.FilesDiscovered,
			});
		});

		await foreach(BackupScanResult result in _scanner.ScanAsync(scanRequest, scanProgress, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			session.AddItem(result.Item);
		}

		// ------------------------------------------------------------
		// 3. Open source (filesystem or media device)
		//    - Establish access to source
		//    - Fail if source is not accessible
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 4. Scan source
		//    - Enumerate directories and files
		//    - Apply include / exclude rules
		//    - Count files and total bytes
		//    - Update progress (phase = Scanning)
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 5. Prepare destination
		//    - Ensure destination is accessible
		//    - Create required directories
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 6. Transfer files
		//    - Copy data from source to destination
		//    - Generate sidecar files
		//    - Update progress (phase = Transferring)
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 7. Execute optional features (if enabled in BackupPlan)
		//    - Hashing
		//    - Metadata extraction
		//    - Verification
		//    - Timestamp correction
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 8. Handle cancellation
		//    - Observe cancellationToken
		//    - Stop processing gracefully if requested
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 9. Finalize backup result
		//    - Collect final counters
		//    - Determine final phase (Completed / Failed / Cancelled)
		//    - Set failure reason if applicable
		// ------------------------------------------------------------

		// ------------------------------------------------------------
		// 10. Return BackupResult
		// ------------------------------------------------------------

		throw new NotImplementedException("BackupEngine is not yet implemented.");
	}
}
