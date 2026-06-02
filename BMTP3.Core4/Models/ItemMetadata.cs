using BMTP3.Core4.Hashing;

namespace BMTP3.Core4.Models;

sealed class ItemMetadata
{
	public Dictionary<HashType, string>? ComputedHashes { get; set; }
	public DateTimeOffset? MediaTakenDateTime { get; set; }

	// Original source dates captured before timestamp correction.
	// These are the dates as they came from the source during scanning.
	public DateTimeOffset? AuthoredDateTime { get; set; }
	public DateTimeOffset? CreatedDateTime { get; set; }
	public DateTimeOffset? ModifiedDateTime { get; set; }
	public DateTimeOffset? AccessedDateTime { get; set; }
}
