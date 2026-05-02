using Microsoft.Extensions.Logging;

namespace BMTP3.Core3.Metadata;

/// <summary>
/// Simple metadata reader that extracts basic file properties.
/// </summary>
public class FileMetadataReader : IMetadataReader
{
	private readonly ILogger<FileMetadataReader> _logger;

	public FileMetadataReader(ILogger<FileMetadataReader> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Dictionary<string, object>> ReadAsync(string filePath, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath);
		ct.ThrowIfCancellationRequested();

		var metadata = new Dictionary<string, object>();

		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException($"File not found: {filePath}");
		}

		try
		{
			var fileInfo = new FileInfo(filePath);

			metadata["FileName"] = fileInfo.Name;
			metadata["FileSize"] = fileInfo.Length;
			metadata["CreatedAt"] = fileInfo.CreationTime;
			metadata["ModifiedAt"] = fileInfo.LastWriteTime;
			metadata["IsReadOnly"] = fileInfo.IsReadOnly;

			_logger.LogDebug("Metadata extracted for {FilePath}: {MetadataCount} fields", filePath, metadata.Count);
			return metadata;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to extract metadata for {FilePath}", filePath);
			throw;
		}
	}
}
