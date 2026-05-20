namespace BMTP3.Core4.Engine.Session;

/// <summary>
/// Identifies the current backup job, that is this session.
/// </summary>
internal sealed record BackupSessionKey(
	string SessionId,
	string SourceIdentity
);