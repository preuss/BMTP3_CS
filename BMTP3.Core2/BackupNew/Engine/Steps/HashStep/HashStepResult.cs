using BMTP3.Core2.BackupNew.Engine.Hashing; // For HashType

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

/// <summary>
/// Represents the result of the HashItemStep for a single item.
/// </summary>
public class HashStepResult
{
	/// <summary>
	/// A dictionary containing the computed hashes for various types.
	/// Key: HashType, Value: Hash string.
	/// </summary>
	public required Dictionary<HashType, string> Hashes { get; init; }

	// Potentially add other result-specific information, like duration of hash computation, etc.
}
