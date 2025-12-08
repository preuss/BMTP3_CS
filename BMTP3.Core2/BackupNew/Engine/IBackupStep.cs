using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps;
/// <summary>
/// Represents a single step in the sequential backup pipeline.
/// </summary>
public interface IBackupStep
{
    /// <summary>
    /// The name of the step (e.g., "Hashing", "Copying", "Decision").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes an operation on a single item.
    /// The method must update the item's State or Metadata.
    /// </summary>
    Task ExecuteAsync(IBackupItem item, BackupPlan jobConfig, CancellationToken ct);
}
