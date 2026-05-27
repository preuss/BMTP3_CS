using BMTP3.Core4.Hashing;

namespace BMTP3.Core4.Models;

sealed class ItemMetadata
{
	public Dictionary<HashType, string>? ComputedHashes { get; set; }

	public DateTimeOffset? AuthoredDateTime { get; set; }
	public DateTimeOffset? CreatedDateTime { get; set; }
	public DateTimeOffset? ModifiedDateTime { get; set; }
	public DateTimeOffset? AccessedDateTime { get; set; }
	public DateTimeOffset? MetadataChangedDateTime { get; set; }
}
