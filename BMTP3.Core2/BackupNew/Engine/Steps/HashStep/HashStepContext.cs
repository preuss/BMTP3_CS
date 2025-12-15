using BMTP3.Core2.BackupNew.Engine.Hashing; // For HashType
using System.Collections.Generic;

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

/// <summary>
/// Contextual configuration for the HashItemStep.
/// Contains parameters specific to how hashing should be performed for a single item.
/// </summary>
public class HashStepContext
{
    /// <summary>
    /// The specific hash types to compute for the item.
    /// </summary>
    public IEnumerable<HashType> HashTypes { get; init; } = new[] { HashType.SHA2_256 };

    /// <summary>
    /// Indicates whether to compute the hash even if it's already present in metadata (e.g., for verification).
    /// </summary>
    public bool ForceRecompute { get; init; } = false;

    // Add other hash-specific parameters here, e.g., parallelism if hash calculation supports it internally
}
