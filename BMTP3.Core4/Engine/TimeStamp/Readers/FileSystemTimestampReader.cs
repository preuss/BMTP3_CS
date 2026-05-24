using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal sealed class FileSystemTimestampReader : ITimestampReader
{
	public IReadOnlyList<TimestampCandidate> Read(FileInfo file, CancellationToken cancellationToken)
	{
		if(!file.Exists)
		{
			return Array.Empty<TimestampCandidate>();
		}

		List<TimestampCandidate> candidates = new();

		// ── Creation Time
		candidates.Add(
			TimestampCandidateFactory.FromDateTime(
				TimestampSourceType.FileSystem,
				TimestampRole.Created,
				file.CreationTimeUtc
			)
		);

		// ── Last Write / Modification Time
		candidates.Add(
			TimestampCandidateFactory.FromDateTime(
				TimestampSourceType.FileSystem,
				TimestampRole.Modified,
				file.LastWriteTimeUtc
			)
		);

		// ── Last Access Time (unknown role)
		candidates.Add(
			TimestampCandidateFactory.FromDateTime(
				TimestampSourceType.FileSystem,
				TimestampRole.Accessed,
				file.LastAccessTimeUtc
			)
		);

		return candidates;
	}
}
