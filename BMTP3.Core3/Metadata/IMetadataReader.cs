namespace BMTP3.Core3.Metadata;

/// <summary>
/// Interface for reading file metadata (timestamps, properties, EXIF).
/// </summary>
public interface IMetadataReader
{
	/// <summary>
	/// Read metadata from a file.
	/// </summary>
	/// <param name="filePath">Path to the file.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Dictionary of metadata key-value pairs.</returns>
	Task<Dictionary<string, object>> ReadAsync(string filePath, CancellationToken ct);
}
