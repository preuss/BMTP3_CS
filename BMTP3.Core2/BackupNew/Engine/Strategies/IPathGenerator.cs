using BMTP3.Core2.BackupNew.Api;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Responsible for calculating the destination path for a backup item.
/// </summary>
public interface IPathGenerator
{
    /// <summary>
    /// Calculates the ideal relative destination path for a given item based on the plan's strategy.
    /// This path is relative to the root OutputPath.
    /// Does not check for collisions or existence.
    /// </summary>
    /// <param name="item">The item being processed (must have Metadata enriched).</param>
    /// <param name="plan">The backup configuration.</param>
    /// <returns>A relative path string (e.g., "2025/12/Photo.jpg").</returns>
    string GenerateRelativePath(IBackupItem item, BackupPlan plan);
}