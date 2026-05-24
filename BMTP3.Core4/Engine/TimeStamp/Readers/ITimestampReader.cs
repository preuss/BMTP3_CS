using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

internal interface ITimestampReader
{
	/// <summary>
	///     Reads timestamp candidates from the given file.
	///     Implementations must be side-effect free and must not throw on malformed metadata.
	/// </summary>
	IReadOnlyList<TimestampCandidate> Read(FileInfo fileInfo, CancellationToken cancellationToken);
}