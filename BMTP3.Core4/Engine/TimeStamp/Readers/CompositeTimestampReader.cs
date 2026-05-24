using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

/// <summary>
///     A master reader that aggregates timestamp candidates from all supported metadata formats.
///     - Each sub-reader MUST return an empty IReadOnlyList&lt;TimestampCandidate&gt; when no candidates are found.
///     - CompositeTimestampReader will continue if an individual reader throws, but will capture the exception.
/// </summary>
internal sealed class CompositeTimestampReader : ITimestampReader
{
	private readonly List<ITimestampReader> _readers;

	public CompositeTimestampReader()
		: this(new List<ITimestampReader>
		{
			new FileSystemTimestampReader(),
			new ExifTimestampReader(),
			new IptcTimestampReader(),
			new GpsTimestampReader(),
			new QuickTimeTimestampReader(),
			new XmpTimestampReader()
		})
	{
	}

	public CompositeTimestampReader(IList<ITimestampReader> readers)
	{
		if(readers == null)
		{
			throw new ArgumentNullException(nameof(readers));
		}
		if(!readers.Any())
		{
			throw new ArgumentException("At least one reader is required.", nameof(readers));
		}

		_readers = new(readers);
	}

	/// <summary>
	///     ITimestampReader contract - returns combined candidates and discards reader errors.
	/// </summary>
	public IReadOnlyList<TimestampCandidate> Read(FileInfo fileInfo, CancellationToken cancellationToken)
	{
		// silently ignore errors when using the parameterless Read method.
		return Read(fileInfo, out IReadOnlyList<(Type ReaderType, Exception Error)> _);
	}

	/// <summary>
	///     Reads timestamp candidates from the given file and outputs a list of any errors encountered.
	/// </summary>
	/// <param name="fileInfo">The file to read metadata from.</param>
	/// <param name="errors">A list of exceptions thrown by individual readers, along with the reader type.</param>
	/// <returns>A combined list of all successfully read candidates.</returns>
	public IReadOnlyList<TimestampCandidate> Read(FileInfo fileInfo, out IReadOnlyList<(Type ReaderType, Exception Error)> errors)
	{
		if(fileInfo == null)
		{
			throw new ArgumentNullException(nameof(fileInfo));
		}

		// Defensive initialisation: ensure 'errors' is never null for normal returns.
		errors = Array.Empty<(Type ReaderType, Exception Error)>();

		List<TimestampCandidate> allCandidates = new();
		List<(Type ReaderType, Exception Error)> encounteredErrors = new();

		foreach(ITimestampReader reader in _readers)
		{
			try
			{
				allCandidates.AddRange(reader.Read(fileInfo, CancellationToken.None));
			} catch(Exception ex)
			{
				// Keep going if a reader throws unexpectedly, but record the error it threw.
				encounteredErrors.Add((reader.GetType(), ex));
			}
		}

		// Expose collected errors (readonly view)
		errors = encounteredErrors.AsReadOnly();
		return allCandidates;
	}
}