using BMTP3.Consoles.candidates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Consoles.exifreader.readers;

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