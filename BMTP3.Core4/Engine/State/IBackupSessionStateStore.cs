namespace BMTP3.Core4.Engine.State;

/// <summary>
/// Provides access to backup session state.
/// Implementations may create a new state or load an existing one.
/// </summary>
internal interface IBackupSessionStateStore
{
	/// <summary>
	/// Opens or creates backup session state identified by the specified key.
	/// </summary>
	Task<BackupSessionState> OpenAsync(
		BackupSessionStateKey key,
		CancellationToken cancellationToken);
}