using BMTP3.Core2.BackupNew.candidates;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

public class QuickTimeTimestampReader : ITimestampReader
{
	private readonly QuickTimeMovieHeaderTimestampReader _movieHeaderReader = new();
	private readonly QuickTimeMetadataHeaderTimestampReader _metadataHeaderReader = new();

	public IReadOnlyList<TimestampCandidate> Read(FileInfo file)
	{
		List<TimestampCandidate> candidates = new();

		candidates.AddRange(_movieHeaderReader.Read(file));
		candidates.AddRange(_metadataHeaderReader.Read(file));

		return candidates;
	}
}
