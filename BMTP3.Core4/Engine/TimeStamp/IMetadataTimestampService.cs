using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.TimeStamp;

internal interface IMetadataTimestampService
{
	Task<IReadOnlyList<DateTimeOffset>> ReadCandidatesAsync(
		IFileInfoSource content,
		CancellationToken cancellationToken
	);
}
