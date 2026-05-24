using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal sealed class QuickTimeTimestampReader : ITimestampReader
{
	private readonly QuickTimeMetadataHeaderTimestampReader _metadataHeaderReader = new();
	private readonly QuickTimeMovieHeaderTimestampReader _movieHeaderReader = new();

	public IReadOnlyList<TimestampCandidate> Read(FileInfo fileInfo, CancellationToken cancellationToken)
	{
		List<TimestampCandidate> candidates = new();

		candidates.AddRange(_movieHeaderReader.Read(fileInfo, cancellationToken));
		candidates.AddRange(_metadataHeaderReader.Read(fileInfo, cancellationToken));

		return candidates;
	}
}