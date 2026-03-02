using BMTP3.Core2.BackupNew.candidates;

namespace BMTP3.Core2.BackupNew.exifreader.readers;

/// <summary>
/// A master reader that aggregates timestamp candidates from all supported metadata formats.
/// </summary>
public class CompositeTimestampReader : ITimestampReader
{
	private readonly List<ITimestampReader> _readers;

	public CompositeTimestampReader()
	{
		// Register all specific readers here (include filesystem reader)
		_readers = new List<ITimestampReader>
		{
			new FileSystemTimestampReader(),
			new ExifTimestampReader(),
			new IptcTimestampReader(),
			new GpsTimestampReader(),
			new QuickTimeTimestampReader(),
			new XmpTimestampReader()
		};
	}

	public IReadOnlyList<TimestampCandidate> Read(FileInfo file)
	{
		List<TimestampCandidate> allCandidates = new();

		foreach(ITimestampReader reader in _readers)
		{
			try
			{
				IReadOnlyList<TimestampCandidate> candidates = reader.Read(file);
				if(candidates != null)
				{
					allCandidates.AddRange(candidates);
				}
			} catch
			{
				// Keep going if a reader throws unexpectedly
			}
		}

		return allCandidates;
	}
}
