namespace BMTP3.Core2.BackupNew.Infrastructure.Repositories;

/// <summary>
///     Persistent record used for resume: identifies a source file and whether it's already saved.
/// </summary>
public class BackupResumeRecord
{
	/// <summary>
	///     Normally a generated unique ID for this item (not from source)
	///     And we use GUID to ensure uniqueness across different source types
	///     Primary key.
	/// </summary>
	public required string ItemId { get; set; }

	/// <summary>
	///     PersistentUniqueId (MTP) or FullName (FileInfo) to identify the source file/object.
	/// </summary>
	public required string SourceId { get; set; }

	/// <summary>
	///     May be full path (FileInfo) or MTP object path.
	///     May collide on MTP/iPhone.
	/// </summary>
	public required string SourcePath { get; set; }

	public required string SourceFileName { get; set; }
	public ulong LengthBytes { get; set; }
	public DateTime? DateCreated { get; set; }
	public DateTime? DateModified { get; set; }
	public DateTime? DateAuthored { get; set; }

	/// <summary>
	///     Persistence outcome, used for resume decision.
	///     Pending is the only state for resume backup.
	/// </summary>
	public required PersistState State { get; set; }

	/// <summary>
	///     When we decided/got it saved (for auditing)
	///     When Saved/Skipped was recorded
	/// </summary>
	public DateTime? BackupDate { get; set; }
}