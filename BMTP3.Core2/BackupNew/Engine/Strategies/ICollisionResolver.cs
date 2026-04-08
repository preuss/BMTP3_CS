using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
///     Responsible for handling file collisions at the destination.
/// </summary>
public interface ICollisionResolver
{
	/// <summary>
	///     Determines the action to take given a proposed target path.
	///     Checks if the file exists, compares content if necessary, and applies renaming strategies.
	/// </summary>
	/// <param name="item">The source item.</param>
	/// <param name="proposedFullPath">The full absolute path where we want to put the file.</param>
	/// <param name="plan">The configuration defining comparison and resolution rules.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>A decision containing the action (Copy, Skip, Rename) and the final approved path.</returns>
	Task<CollisionResult> ResolveAsync(IBackupItem item, string proposedFullPath, BackupPlan plan,
		CancellationToken ct);
}