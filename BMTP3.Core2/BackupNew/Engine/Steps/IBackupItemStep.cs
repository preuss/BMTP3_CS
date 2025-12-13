using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps;
/// <summary>
/// Represents a single step in the sequential backup pipeline.
/// </summary>
public interface IBackupItemStep<TContext, TResult>
{
    /// <summary>
    /// The name of the step (e.g., "Hashing", "Copying", "Decision").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes an operation on a single item.
    /// The method must update the item's State or Metadata.
    /// </summary>
    Task<TResult> ExecuteAsync(TContext input, IBackupItem item, CancellationToken ct);
}