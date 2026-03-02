using BMTP3.Core2.BackupNew.candidates;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

public sealed class FileSystemTimestampReader : ITimestampReader
{
	public IReadOnlyList<TimestampCandidate> Read(FileInfo file)
	{
		if(file is null) throw new ArgumentNullException(nameof(file));
		if(!file.Exists) return Array.Empty<TimestampCandidate>();

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

		// ── Last Access Time (ukendt rolle)
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