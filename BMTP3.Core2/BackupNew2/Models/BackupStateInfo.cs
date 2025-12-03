namespace BMTP3.Core2.BackupNew2.Models;

/// <summary>
/// A lightweight record representing the persistent state of a backup item.
/// Used by the Repository to store history (Resume functionality).
/// </summary>
public class BackupStateInfo
{
	public string OriginalSourcePath { get; set; } = string.Empty;
	public string HashSha256 { get; set; } = string.Empty;
	public long SizeBytes { get; set; }
	public DateTime AuthoredDate { get; set; }
	public BackupTerminalState TerminalState { get; set; }
	public DateTime ProcessedDate { get; set; }
}
