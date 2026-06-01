using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Readers;

/// <summary>
///     Aggregates timestamp candidates from all supported metadata formats
///     (filesystem dates, EXIF, XMP, IPTC, GPS, QuickTime).
///     Individual sub-reader failures are collected and do not interrupt the overall read.
/// </summary>
/// <remarks>
///     Implements <see cref="ICompositeTimestampReader"/> which extends <see cref="ITimestampReader"/>.
///     <list type="bullet">
///         <item>
///             <see cref="Read"/> — throws <see cref="TimestampReaderException"/>
///             when <em>all</em> sub-readers fail (fail-fast).
///         </item>
///         <item>
///             <see cref="TryReadCollect"/> — never throws from sub-reader errors;
///             returns <c>false</c> when all sub-readers fail, and reports
///             per-reader errors via <c>out</c> parameters.
///         </item>
///     </list>
/// </remarks>
internal sealed class CompositeTimestampReader : ICompositeTimestampReader
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
		if (readers == null)
		{
			throw new ArgumentNullException(nameof(readers));
		}
		if (!readers.Any())
		{
			throw new ArgumentException("At least one reader is required.", nameof(readers));
		}

		_readers = new(readers);
	}

	/// <summary>
	///     Reads timestamp candidates from all sub-readers.
	/// </summary>
	/// <exception cref="TimestampReaderException">
	///     Thrown when <em>all</em> sub-readers fail to produce any candidates.
	///     The inner exception is an <see cref="AggregateException"/> containing
	///     each individual <see cref="TimestampReaderException"/>.
	/// </exception>
	public IReadOnlyList<TimestampCandidate> Read(FileInfo fileInfo, CancellationToken cancellationToken)
	{
		if (!TryReadCollect(fileInfo, out var candidates, out var errors, cancellationToken))
		{
			throw new TimestampReaderException(typeof(CompositeTimestampReader), "All sub-readers failed to produce timestamp candidates.", new AggregateException(errors));
		}
		return candidates;
	}

	/// <summary>
	///     Attempts to read timestamp candidates from all sub-readers.
	///     Individual reader failures are collected in <paramref name="errors"/>
	///     and do not interrupt reading from other readers.
	/// </summary>
	/// <param name="fileInfo">The file to read metadata from.</param>
	/// <param name="candidates">All successfully read candidates, or an empty list if all readers failed.</param>
	/// <param name="errors">
	///     Per-reader errors wrapped in <see cref="TimestampReaderException"/>.
	///     Empty when all readers succeeded or returned no candidates without error.
	/// </param>
	/// <param name="cancellationToken">Cancellation token passed to each sub-reader.</param>
	/// <returns>
	///     <c>true</c> if at least one sub-reader produced candidates;
	///     <c>false</c> if all sub-readers failed.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="fileInfo"/> is <c>null</c>.</exception>
	/// <exception cref="FileNotFoundException"><paramref name="fileInfo"/> does not exist.</exception>
	/// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is cancelled.</exception>
	public bool TryReadCollect(
		FileInfo fileInfo,
		out IReadOnlyList<TimestampCandidate> candidates,
		out IReadOnlyList<TimestampReaderException> errors,
		CancellationToken cancellationToken)
	{
		if (fileInfo == null)
		{
			throw new ArgumentNullException(nameof(fileInfo));
		}

		if (!fileInfo.Exists)
		{
			throw new FileNotFoundException("The specified file does not exist.", fileInfo.FullName);
		}

		List<TimestampCandidate> allCandidates = new();
		List<TimestampReaderException> encounteredErrors = new();

		foreach (ITimestampReader reader in _readers)
		{
			try
			{
				allCandidates.AddRange(reader.Read(fileInfo, cancellationToken));
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				encounteredErrors.Add(new TimestampReaderException(reader.GetType(), ex.Message, ex));
			}
		}

		candidates = allCandidates.AsReadOnly();
		errors = encounteredErrors.AsReadOnly();
		return allCandidates.Count > 0;
	}
}
