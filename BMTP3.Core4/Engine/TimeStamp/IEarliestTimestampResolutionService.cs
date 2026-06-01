namespace BMTP3.Core4.Engine.TimeStamp;

/// <summary>
///     Resolves the earliest valid timestamp from embedded metadata (EXIF, XMP, QuickTime, etc.)
///     for a given piece of content, optionally applies it to the target file and updates
///     item and metadata properties.
///     <para>
///         <see cref="ResolveAndApplyEarliestAsync" /> accepts an <see cref="EarliestTimestampResolutionRequest" />
///         containing the source content (for metadata extraction), the target file to correct,
///         and the item/metadata objects to update.
///         When <paramref name="enableTimestampCorrection" /> is <c>true</c>, the resolved
///         timestamp is written to <see cref="EarliestTimestampResolutionRequest.TimestampCorrectionTarget" />
///         and <see cref="EarliestTimestampResolutionRequest.Item" /> date properties are updated.
///         <see cref="EarliestTimestampResolutionRequest.Metadata" /> is always updated when a timestamp is resolved.
///     </para>
/// </summary>
internal interface IEarliestTimestampResolutionService
{
	/// <summary>
	///     Scans all metadata readers for timestamp candidates, resolves the earliest
	///     valid timestamp, applies it to the target file (if enabled), and updates
	///     item and metadata properties.
	/// </summary>
	/// <param name="request">
	///     Contains the content for metadata extraction, the target file to correct,
	///     and the item/metadata objects to update.
	/// </param>
	/// <param name="enableTimestampCorrection">
	///     If <c>true</c>, the resolved timestamp is written to the target file's
	///     filesystem attributes and the item's date properties are updated.
	/// </param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>
	///     An <see cref="EarliestTimestampResolutionResult" /> containing the earliest timestamp
	///     (or <c>null</c> if none could be resolved) and the candidate that produced it.
	/// </returns>
	Task<EarliestTimestampResolutionResult> ResolveAndApplyEarliestAsync(
		EarliestTimestampResolutionRequest request,
		bool enableTimestampCorrection,
		CancellationToken cancellationToken
	);
}
