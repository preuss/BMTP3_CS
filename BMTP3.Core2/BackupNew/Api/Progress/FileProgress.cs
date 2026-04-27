using System;
using BMTP3.Core2.BackupNew.Api.Progress.Enums;

namespace BMTP3.Core2.BackupNew.Api.Progress;

/// <summary>
///     Represents the progress of a single file currently being processed.
/// </summary>
public class FileProgress
{
	/// <summary>
	///     The name of the file (e.g., "photo.jpg").
	/// </summary>
	public string FileName { get; init; } = string.Empty;
	/// <summary>
	///     The relative path from the source root (e.g., "DCIM/Camera/photo.jpg").
	/// </summary>
	public string RelativePath { get; init; } = string.Empty;
	/// <summary>
	///     The full source path or unique identifier.
	/// </summary>
	public string SourcePath { get; init; } = string.Empty;
	/// <summary>
	///     The current processing phase of the file.
	/// </summary>
	public FilePhase Phase { get; init; }
	/// <summary>
	///     The total size of the file in bytes.
	/// </summary>
	public long BytesTotal { get; init; }
	/// <summary>
	///     The number of bytes processed so far.
	/// </summary>
	public long BytesProcessed { get; init; }
	/// <summary>
	///     Completion percentage (0–100).
	/// </summary>
	public double Percent => BytesTotal > 0 ? (double)BytesProcessed / BytesTotal * 100.0 : 0;
	/// <summary>
	///     Which retry attempt this is. 0 = first attempt.
	/// </summary>
	public int RetryAttempt { get; init; }
	/// <summary>
	///     When processing of this file started.
	/// </summary>
	public DateTimeOffset StartedAt { get; init; }
}
