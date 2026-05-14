namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Defines the current processing phase of a single file during backup.
/// Each phase represents a distinct step in the file processing pipeline.
/// <see cref="BackupProgressItem.BytesProcessed"/> is only relevant during phases
/// that involve byte-level work (Transferring, Hashing).
/// </summary>
public enum BackupProgressItemPhase
{
	/// <summary>
	/// The file is being copied from the source to a temporary working directory.
	/// This involves reading from the source (disk or MTP device) and writing to a temp file.
	/// Duration depends on file size and source speed.
	/// <see cref="BackupProgressItem.BytesProcessed"/> indicates progress from 0 to <see cref="BackupProgressItem.Length"/>.
	/// </summary>
	Transferring,

	/// <summary>
	/// Metadata is being extracted and processed for the file.
	/// This includes reading EXIF data (camera make/model, GPS, etc.) and
	/// correcting the file's timestamps based on the best available date source
	/// (EXIF DateTimeOriginal → CreateDate → QuickTimeCreated → filesystem).
	/// Duration is typically a few seconds per file regardless of file size.
	/// <see cref="BackupProgressItem.BytesProcessed"/> is not relevant during this phase.
	/// </summary>
	ProcessingMetadata,

	/// <summary>
	/// A cryptographic hash is being computed for the file.
	/// The hash is stored in the sidecar file for future integrity verification.
	/// Duration depends on file size and CPU speed.
	/// <see cref="BackupProgressItem.BytesProcessed"/> indicates progress from 0 to <see cref="BackupProgressItem.Length"/>.
	/// </summary>
	Hashing,

	/// <summary>
	/// Finalizing the file processing.
	/// This includes writing the sidecar (.sidecar.json) with metadata and hashes,
	/// resolving the final destination path (handling naming collisions),
	/// and atomically moving the temp file(s) to their final location.
	/// Duration is typically instant regardless of file size.
	/// <see cref="BackupProgressItem.BytesProcessed"/> is not relevant during this phase.
	/// </summary>
	Finalizing
}
