using BMTP3.Core4.Engine.TimeStamp.Readers;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.TimeStamp;

internal sealed class MetadataTimestampService : IMetadataTimestampService
{
	private readonly CompositeTimestampReader _reader;

	public MetadataTimestampService()
	{
		_reader = new CompositeTimestampReader();
	}

	public Task<IReadOnlyList<DateTimeOffset>> ReadCandidatesAsync(
		IFileInfoSource content,
		CancellationToken cancellationToken
	)
	{
		if(!content.TryGetFileInfo(out FileInfo file))
		{
			return Task.FromResult<IReadOnlyList<DateTimeOffset>>(Array.Empty<DateTimeOffset>());
		}

		IReadOnlyList<Candidates.TimestampCandidate> candidates = _reader.Read(file, cancellationToken);

		List<DateTimeOffset> results = new(candidates.Count);

		foreach(Candidates.TimestampCandidate candidate in candidates)
		{
			if(candidate.TryToDateTimeOffset(out DateTimeOffset dto))
			{
				results.Add(dto);
			}
		}

		return Task.FromResult<IReadOnlyList<DateTimeOffset>>(results);
	}
}
