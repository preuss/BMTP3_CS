using BMTP3.Core4.Hashing;

namespace BMTP3.Core4.Models;

sealed class ItemMetadata
{
	public Dictionary<HashType, string>? ComputedHashes { get; set; }
	public DateTimeOffset? ResolvedDateTime { get; set; }
}
