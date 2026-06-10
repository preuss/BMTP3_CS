using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Api.Models;

/// <summary>
/// Represents progress information for a file that is currently active
/// in the backup workflow.
/// </summary>
public sealed record BackupProgressItem
{
	/// <summary>
	/// The source-relative path of the file.
	/// </summary>
	public string RelativeFilePath { get; init; } = string.Empty;

	/// <summary>
	/// The size of the file in bytes, if known.
	/// </summary>
	public long Length { get; init; }

	/// <summary>
	/// The number of bytes processed for this file so far.
	/// Only relevant during <see cref="BackupProgressItemPhase.Transferring"/> and
	/// <see cref="BackupProgressItemPhase.Hashing"/> phases, where it progresses from 0 to <see cref="Length"/>.
	/// Null during phases without byte-level work (<see cref="BackupProgressItemPhase.ProcessingMetadata"/>
	/// and <see cref="BackupProgressItemPhase.Finalizing"/>).
	/// Check <see cref="Phase"/> to determine if this value should be displayed.
	/// </summary>
	public long? BytesProcessed { get; init; }

	/// <summary>
	/// Gets the current phase of the backup operation.
	/// </summary>
	public BackupProgressItemPhase Phase { get; init; }
}