namespace BMTP3.Core4.Engine.State;

/// <summary>
/// Identifies the backup session state to open or create.
/// </summary>
internal sealed record BackupSessionStateKey(
	string SessionId,
	string SourceIdentity
);