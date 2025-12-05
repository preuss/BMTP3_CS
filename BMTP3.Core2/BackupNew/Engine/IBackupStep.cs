using BMTP3.Core2.BackupNew.Models;
using BMTP3.Core2.BackupNew.Models.Configuration;

namespace BMTP3.Core2.BackupNew.Engine;

/// <summary>
/// Represents a single step in the sequential backup pipeline.
/// </summary>
public interface IBackupStep
{
	/// <summary>
	/// The unique name of the step (e.g., "FileAnalysis").
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Executes the step logic on the given backup item.
	/// </summary>
	/// <param name="item">The item to process.</param>
	/// <param name="jobConfig">The global configuration for the current job.</param>
	/// <param name="ct">Cancellation token.</param>
	Task ExecuteAsync(IBackupItem item, BackupJob jobConfig, CancellationToken ct);
}
