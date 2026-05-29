using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.TimeStamp;

internal sealed record EarliestTimestampResolutionRequest
{
	public required IContent Content { get; init; }
	public required FileInfo TimestampCorrectionTarget { get; init; }
	public required BackupItem Item { get; init; }
	public required ItemMetadata Metadata { get; init; }
}
