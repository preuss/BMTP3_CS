using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Robust implementation of IMetadataReader using MetadataExtractor.
/// Implements the "Timestamp Waterfall" logic: EXIF > MTP > Created > Modified.
/// </summary>
public class MetadataReader : IMetadataReader
{
	private readonly ILogger<MetadataReader> _logger;

	public MetadataReader(ILogger<MetadataReader> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNull(item.Content);

		// 1. Ensure basic file info is present
		if(!item.Metadata.Has(MetadataKey.Length))
		{
			item.Metadata.Set(MetadataKey.Length, item.Content.Length);
		}

		// Only attempt to read file headers if we have a physical file we can read randomly/efficiently.
		// At this stage in the pipeline (after Staging), the content should be a FileContent pointing to a local temp file.
		if(item.Content is FileContent fileContent)
		{
			await ExtractInternalMetadataAsync(item, fileContent.FileInfo.FullName, ct);
		} else
		{
			_logger.LogWarning("Skipping internal metadata extraction for item {ItemId}. Content is not a local file (Type: {Type}).", item.Id, item.Content.GetType().Name);
		}

		// 3. Finalize AuthoredDateTime based on Waterfall Logic
		ApplyTimestampWaterfall(item);
	}

	private static readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".jpg", ".jpeg", ".tiff", ".tif", ".png", ".gif", ".bmp", ".webp", ".heic", ".heif", ".mp4", ".mov", ".avi", ".mkv"
	};
	private static readonly bool _usedOnlySupportedExtensions = false;
	/// <summary>
	/// Quick guard: skip heavy parsing for unsupported extensions to avoid noisy exceptions.
	/// </summary>
	/// <param name="ext"></param>
	/// <returns></returns>
	private static bool ContinueExtractionGuard(string ext)
	{
		if(!_usedOnlySupportedExtensions)
		{
			return true;
		}
		if(string.IsNullOrEmpty(ext))
		{
			return false;
		}
		return _supportedExtensions.Contains(ext);
	}
	private async Task ExtractInternalMetadataAsync(IBackupItem item, string filePath, CancellationToken ct)
	{
		try
		{

			if(!ContinueExtractionGuard(Path.GetExtension(filePath)))
			{
				string? ext = Path.GetExtension(filePath);
				_logger.LogDebug("Skipping internal metadata extraction for unsupported extension '{Ext}' (Item {ItemId}).", ext, item.Id);
				return;
			}

			// Run on thread pool to avoid blocking the pipeline with heavy parsing
			await Task.Run(() =>
			{
				if(!File.Exists(filePath)) return;

				try
				{
					var directories = ImageMetadataReader.ReadMetadata(filePath);
					var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
					var ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
					var quickTimeDirectory = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();

					DateTime? exifDate = null;

					// A. Try EXIF SubIFD (Most precise for photos)
					if(subIfdDirectory != null)
					{
						if(subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dt))
							exifDate = dt;
						else if(subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out dt))
							exifDate = dt;
					}

					// B. Try EXIF IFD0 (Fallback for photos)
					if(exifDate == null && ifd0Directory != null)
					{
						if(ifd0Directory.TryGetDateTime(ExifDirectoryBase.TagDateTime, out DateTime dt))
							exifDate = dt;
					}

					// C. Try QuickTime (For MOV/MP4 from iPhones etc.)
					if(exifDate == null && quickTimeDirectory != null)
					{
						if(quickTimeDirectory.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out DateTime dt))
							exifDate = dt;
					}

					// Store if found
					if(exifDate.HasValue && exifDate.Value != DateTime.MinValue)
					{
						item.Metadata.Set(MetadataKey.RawExifDateTaken, exifDate.Value);
						_logger.LogDebug("Found EXIF Date for {ItemId}: {Date}", item.Id, exifDate.Value);
					}

					// D. Try to extract Model
					string? model = null;
					if(ifd0Directory != null)
					{
						model = ifd0Directory.GetString(ExifDirectoryBase.TagModel);
					}

					if(!string.IsNullOrWhiteSpace(model))
					{
						item.Metadata.Set(MetadataKey.Model, model.Trim());
					}
				} catch(ImageProcessingException imgEx)
				{
					// MetadataExtractor couldn't parse the file format even though extension matched.
					_logger.LogWarning(imgEx, "Image processing failed for item {ItemId} ({Path}). Skipping internal metadata.", item.Id, filePath);
					item.AddLog($"Metadata Extraction Failed (image parse): {imgEx.Message}", "MetadataReader");
				}
			}, ct);
		} catch(Exception ex)
		{
			// We do NOT fail the backup just because we couldn't parse EXIF. We log and fall back.
			string sourcePath = item.Metadata.Get<string>(MetadataKey.SourceFullPath) ?? "unknown";
			_logger.LogWarning(ex, "Failed to extract internal metadata for item {ItemId} ({Path}). Using fallback dates.", item.Id, sourcePath);
			item.AddLog($"Metadata Extraction Failed: {ex.Message}", "MetadataReader");
		}
	}

	private void ApplyTimestampWaterfall(IBackupItem item)
	{
		// 1. EXIF (Highest Priority)
		if(item.Metadata.Has(MetadataKey.RawExifDateTaken))
		{
			var exifDate = item.Metadata.Get<DateTime>(MetadataKey.RawExifDateTaken);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, exifDate);
			item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.Exif);
			item.AddLog($"Timestamp set from EXIF: {exifDate}", "TimestampCorrection");
			return;
		}

		// 2. MTP (Medium Priority)
		if(item.Metadata.Has(MetadataKey.RawMtpAuthoredDate))
		{
			var mtpDate = item.Metadata.Get<DateTime>(MetadataKey.RawMtpAuthoredDate);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, mtpDate);
			item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.Mtp);
			item.AddLog($"Timestamp set from MTP: {mtpDate}", "TimestampCorrection");
			return;
		}

		// 3. FileSystem Created (Low Priority)
		if(item.Metadata.Has(MetadataKey.CreatedDateTime))
		{
			var created = item.Metadata.Get<DateTime>(MetadataKey.CreatedDateTime);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, created);
			item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.FileSystem);
			item.AddLog($"Timestamp set from FS Created: {created}", "TimestampCorrection");
			return;
		}

		// 4. FileSystem Modified (Lowest Priority)
		if(item.Metadata.Has(MetadataKey.ModifiedDateTime))
		{
			var mod = item.Metadata.Get<DateTime>(MetadataKey.ModifiedDateTime);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, mod);
			item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.LastModified);
			item.AddLog($"Timestamp set from FS Modified: {mod}", "TimestampCorrection");
			return;
		}

		// 5. Fallback
		var now = DateTime.UtcNow;
		item.Metadata.Set(MetadataKey.AuthoredDateTime, now);
		item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.Unknown);
		item.AddLog("Timestamp fallback to UTC Now", "TimestampCorrection");
	}
}