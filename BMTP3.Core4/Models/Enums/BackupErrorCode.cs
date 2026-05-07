namespace BMTP3.Core4.Models.Enums;

/// <summary>
/// Defines the reason why a backup job failed in a controlled and handled manner.
/// These error codes represent terminal, domain-level conditions that caused
/// the backup job to stop, but were fully understood and handled by Core4.
/// </summary>
public enum BackupErrorCode
{
	// ---------------------------------------------------------------------
	// Configuration & setup
	// ---------------------------------------------------------------------

	/// <summary>
	/// The provided backup plan was invalid or incomplete.
	/// </summary>
	InvalidConfiguration,

	/// <summary>
	/// The configured source could not be found or accessed.
	/// </summary>
	SourceNotFound,

	/// <summary>
	/// The configured destination could not be accessed.
	/// </summary>
	DestinationNotAccessible,


	// ---------------------------------------------------------------------
	// Scanning
	// ---------------------------------------------------------------------

	/// <summary>
	/// Scanning the source failed due to an unrecoverable condition.
	/// </summary>
	ScanFailed,

	/// <summary>
	/// Required access to the source was denied.
	/// </summary>
	AccessDenied,


	// ---------------------------------------------------------------------
	// Transfer
	// ---------------------------------------------------------------------

	/// <summary>
	/// File transfer failed and the backup could not continue.
	/// </summary>
	TransferFailed,

	/// <summary>
	/// The destination ran out of disk space.
	/// </summary>
	InsufficientDiskSpace,


	// ---------------------------------------------------------------------
	// Media device (MTP)
	// ---------------------------------------------------------------------

	/// <summary>
	/// The media device became unavailable during the backup.
	/// </summary>
	MediaDeviceDisconnected,


	// ---------------------------------------------------------------------
	// Optional features
	// ---------------------------------------------------------------------

	/// <summary>
	/// File hashing failed and caused the backup to stop.
	/// </summary>
	HashingFailed,

	/// <summary>
	/// Metadata extraction failed and caused the backup to stop.
	/// </summary>
	MetadataExtractionFailed,

	/// <summary>
	/// Post-transfer verification failed.
	/// </summary>
	VerificationFailed,

	/// <summary>
	/// Restoring original timestamps failed.
	/// </summary>
	TimestampCorrectionFailed
}