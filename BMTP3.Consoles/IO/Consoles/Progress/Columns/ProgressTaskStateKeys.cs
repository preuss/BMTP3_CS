namespace BMTP3.Consoles.IO.Consoles.Progress.Columns;

/// <summary>
/// Shared <see cref="ProgressTask.State"/> keys written by the progress display
/// and read by the progress columns.
/// </summary>
internal static class ProgressTaskStateKeys
{
	/// <summary>True when the task tracks a single active file, false for overall progress.</summary>
	public const string IsByteTask = "IsByteTask";

	/// <summary>Raw <see cref="BMTP3.Core4.Api.Models.Enums.BackupProgressItemPhase"/> of the active file.</summary>
	public const string ActiveFilePhase = "ActiveFilePhase";

	/// <summary>Raw <see cref="BMTP3.Core4.Api.Models.Enums.BackupProgressPhase"/> of the overall run.</summary>
	public const string OverallPhase = "OverallPhase";

	/// <summary>Total bytes processed so far (overall task).</summary>
	public const string TotalBytesProcessed = "TotalBytesProcessed";

	/// <summary>Total bytes selected for the run (overall task).</summary>
	public const string TotalBytesSelected = "TotalBytesSelected";
}
