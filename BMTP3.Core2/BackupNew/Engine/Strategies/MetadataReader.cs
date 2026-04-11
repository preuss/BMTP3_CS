using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using Microsoft.Extensions.Logging;
using Directory = MetadataExtractor.Directory;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
///     Robust implementation of IMetadataReader using MetadataExtractor.
///     Delegates timestamp selection to an injected <see cref="ITimestampWaterfall" />.
/// </summary>
public class MetadataReader : IMetadataReader
{
	private static readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		// Common raster image formats
		".jpg", ".jpeg", ".jpe", ".jfif", ".jp2", ".jpx",

		".png", ".gif", ".bmp", ".webp", ".svg",

		// HEIF / HEIC
		".heic", ".heif",

		// TIFF
		".tiff", ".tif",

		// Camera RAW formats (common manufacturers)
		".cr2", ".cr3", ".nef", ".nrw", ".arw", ".orf", ".raf", ".rw2", ".dng", ".pef", ".sr2", ".srw",

		// Photoshop / other image containers
		".psd",

		// Video containers
		".mp4", ".m4v", ".mov", ".avi", ".mkv", ".webm", ".3gp", ".3g2", ".wmv", ".mts", ".m2ts"
	};

	// When true, restrict extraction attempts to the extensions listed in _supportedExtensions.
	private static readonly bool _restrictToSupportedExtensions = true;
	private readonly ILogger<MetadataReader> _logger;
	private readonly ITimestampWaterfall _timestampWaterfall;

	public MetadataReader(ILogger<MetadataReader> logger, ITimestampWaterfall timestampWaterfall)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_timestampWaterfall = timestampWaterfall ?? throw new ArgumentNullException(nameof(timestampWaterfall));
	}

	public async Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNull(item.Content);

		// 1. Ensure basic file info is present
		if(item.Metadata.Has(MetadataKey.Length) == false)
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
		_timestampWaterfall.Apply(item);
	}

	/// <summary>
	///     Check whether the given extension is one of the supported extensions.
	///     Accepts null/empty and returns false for those.
	/// </summary>
	private static bool IsSupportedExtension(string? ext)
	{
		if(string.IsNullOrWhiteSpace(ext))
		{
			return false;
		}

		return _supportedExtensions.Contains(ext);
	}

	/// <summary>
	///     Decide whether we should attempt internal metadata extraction for the given extension.
	///     Returns true when extraction is allowed.
	/// </summary>
	private static bool TryExtract(string? ext)
	{
		// If not restricting, allow extraction for all files.
		if(_restrictToSupportedExtensions == false)
		{
			return true;
		}

		// Otherwise only allow if extension is in the whitelist.
		return IsSupportedExtension(ext);
	}

	private async Task ExtractInternalMetadataAsync(IBackupItem item, string filePath, CancellationToken ct)
	{
		try
		{
			// Prefer the original source file extension (if present) because staging often renames files to .bin/.tmp
			string? sourcePath = item.Metadata.Get<string>(MetadataKey.SourceFullPath);
			string extToCheck;
			if(string.IsNullOrEmpty(sourcePath) == false)
			{
				extToCheck = Path.GetExtension(sourcePath);
			} else
			{
				extToCheck = Path.GetExtension(filePath);
			}

			if(TryExtract(extToCheck) == false)
			{
				string ext = extToCheck;
				_logger.LogDebug("Skipping internal metadata extraction for unsupported extension '{Ext}' (Item {ItemId}).", ext, item.Id);
				return;
			}

			// Run on thread pool to avoid blocking the pipeline with heavy parsing
			await Task.Run(() =>
			{
				if(File.Exists(filePath) == false)
				{
					return;
				}

				try
				{
					// 1. Read timestamps using the CompositeTimestampReader
					FileInfo fileInfo = new FileInfo(filePath);
					BMTP3.Core2.BackupNew.exifreader.readers.CompositeTimestampReader compositeReader = new();

					IReadOnlyList<(Type ReaderType, Exception Error)> readerErrors;
					IReadOnlyList<BMTP3.Core2.BackupNew.candidates.TimestampCandidate> candidates = compositeReader.Read(fileInfo, out readerErrors);

					foreach ((Type ReaderType, Exception Error) error in readerErrors)
					{
						_logger.LogWarning(error.Error, "Timestamp reader {ReaderType} failed for item {ItemId}", error.ReaderType.Name, item.Id);
						item.AddLog($"Reader {error.ReaderType.Name} failed: {error.Error.Message}", "MetadataReader");
					}

					DateTime? bestDate = candidates
						.Where(c => c.IsValidComposition() && c.TryToDateTime(out DateTime _))
						.Select(c =>
						{
							c.TryToDateTime(out DateTime dt);
							return (DateTime?)dt;
						})
						.FirstOrDefault();

					if (bestDate.HasValue && bestDate.Value != DateTime.MinValue)
					{
						item.Metadata.Set(MetadataKey.RawExifDateTaken, bestDate.Value);
						_logger.LogDebug("Found EXIF Date via CompositeReader for {ItemId}: {Date}", item.Id, bestDate.Value);
					}

					// 2. Extract specific physical device metadata (Model)
					IReadOnlyList<Directory> directories = ImageMetadataReader.ReadMetadata(filePath);
					ExifIfd0Directory? ifd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

					string? model = null;
					if (ifd0Directory != null)
					{
						model = ifd0Directory.GetString(ExifDirectoryBase.TagModel);
					}

					if (string.IsNullOrWhiteSpace(model) == false)
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
}