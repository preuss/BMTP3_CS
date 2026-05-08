using BMTP3.Core4.Helpers;

namespace BMTP3.Core4.Engine.State;

/// <summary>
/// Creates a new in-memory backup session state.
/// This implementation does not persist state.
/// </summary>
internal sealed class InMemoryBackupSessionStateStore : IBackupSessionStateStore
{
	public Task<BackupSessionState> OpenAsync(
		BackupSessionStateKey key,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		key = Guard.RequireNonNull(key);

		BackupSessionState session = new(
			key.SessionId,
			key.SourceIdentity);

		return Task.FromResult(session);
	}
}