using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Sidecar.Document;
using BMTP3.Core4.Engine.Sidecar.Writers;
using BMTP3.Core4.Hashing;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core4.Engine.Sidecar;

internal sealed class SidecarService : ISidecarService
{
	private readonly ILogger<SidecarService> _logger;

	public SidecarService(ILogger<SidecarService> logger)
	{
		_logger = logger;
	}

	public async Task WriteAsync(string targetFilePath, SidecarRequest request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentException.ThrowIfNullOrWhiteSpace(targetFilePath);

		try
		{
			SidecarDocument document = BuildDocument(request);
			ISidecarWriter writer = ResolveWriter(request.Format);

			string extension = request.Format switch
			{
				SidecarFormat.Ini => ".ini",
				SidecarFormat.Json => ".json",
				_ => throw new InvalidOperationException($"Unsupported sidecar format: {request.Format}"),
			};

			string sidecarPath = $"{targetFilePath}.sidecar{extension}";

			await using FileStream fileStream = new(sidecarPath, FileMode.Create, FileAccess.Write, FileShare.None);
			await writer.WriteToStreamAsync(document, fileStream, cancellationToken);

			_logger.LogDebug("Sidecar written: {SidecarPath}", sidecarPath);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Sidecar generation failed for {TargetPath}", targetFilePath);
			throw;
		}
	}

	private static SidecarDocument BuildDocument(SidecarRequest request)
	{
		SidecarDocument doc = new()
		{
			HeaderComment =
			[
				"Sidecar for one backup file.",
				"This INI file describes the source file, backup time, relative paths, and hashes.",
				$"All dates in [Source] are dates from the source, not from the backup file in the destination.",
			],
		};

		// ------------------------------------------------------------
		// [Source]
		// ------------------------------------------------------------
		doc.WithSection("Source", weight: 10,
			comment: "[Source] is the central section.\nIt holds common source file information:\nsource type, source identity, source file name, and all source dates.")
			.WithProperty("SourceType", request.SourceType, comment: "Either MtpDevice or Drive.")
			.WithProperty("SourcePersistentUniqueId", request.SourcePersistentUniqueId, comment: "Stable identity for the source file, if available.")
			.WithProperty("SourceFileName", request.SourceFileName, comment: "The file name of the source file.")
			.WithProperty("MediaTakenDateTime", request.MediaTakenDateTime, comment: "The resolved media date for the source file.\nThis is the date previously referred to as the resolved media datetime.")
			.WithProperty("AuthoredDateTime", request.AuthoredDateTime, comment: "Special source date commonly found in MTP device metadata.\nIt may not correspond to any regular filesystem date.")
			.WithProperty("CreateDateTime", request.CreateDateTime, comment: "Standard source filesystem dates.")
			.WithProperty("LastWriteDateTime", request.LastWriteDateTime)
			.WithProperty("LastAccessDateTime", request.LastAccessDateTime);

		// ------------------------------------------------------------
		// [SourceDevice] or [SourceDrive] — only when SourceDetails exist
		// ------------------------------------------------------------
		if (request.SourceDetails is { Count: > 0 } && request.SourceDetailsSectionName is not null)
		{
			string? sectionComment = request.SourceDetailsSectionName switch
			{
				"SourceDevice" => "SourceDevice is used only when SourceType=MtpDevice. It contains device details for the MTP source.",
				"SourceDrive" => "SourceDrive is used only when SourceType=Drive. It contains drive information.",
				_ => null,
			};

			SidecarSection detailsSection = doc.WithSection(request.SourceDetailsSectionName, weight: 20, comment: sectionComment);

			foreach (KeyValuePair<string, string> detail in request.SourceDetails)
			{
				detailsSection.WithProperty(detail.Key, detail.Value);
			}
		}

		// ------------------------------------------------------------
		// [Backup]
		// ------------------------------------------------------------
		doc.WithSection("Backup", weight: 30, comment: "Backup contains only the backup information that is useful per file.")
			.WithProperty("BackupStartDateTime", request.BackupStartDateTime);

		// ------------------------------------------------------------
		// [Path]
		// ------------------------------------------------------------
		doc.WithSection("Path", weight: 40,
			comment: "Path describes relative paths, always including the file name. SourceRelativePath is the original source-relative path. SanitizedSourceRelativePath is the Windows-compatible version of the source path. TargetRelativePath is the actual relative path in the destination used by this backup.")
			.WithProperty("SourceRelativePath", request.SourceRelativePath)
			.WithProperty("SanitizedSourceRelativePath", request.SanitizedSourceRelativePath)
			.WithProperty("TargetRelativePath", request.TargetRelativePath);

		// ------------------------------------------------------------
		// [Hashes] — always all keys, even when empty
		// ------------------------------------------------------------
		SidecarSection hashesSection = doc.WithSection("Hashes", weight: 50,
			comment: "Hashes must always include all keys, even when a hash value is empty. This ensures a stable and predictable sidecar format.");

		HashType[] allHashTypes =
		[
			HashType.SHA3_512_FIPS202,
			HashType.SHA3_512_KECCAK,
			HashType.SHA2_512,
			HashType.SHA2_256,
			HashType.MD5_128,
			HashType.BLAKE3_256,
			HashType.BLAKE3_512,
		];

		string? fips202Value = request.Hashes?.GetValueOrDefault(HashType.SHA3_512_FIPS202);

		// SHA3_512 is an alias for SHA3_512_FIPS202
		hashesSection.WithProperty("SHA3_512", fips202Value);

		foreach (HashType hashType in allHashTypes)
		{
			string? value = request.Hashes?.GetValueOrDefault(hashType) ?? string.Empty;
			string keyName = hashType switch
			{
				HashType.MD5_128 => "MD5",
				_ => hashType.ToString(),
			};
			hashesSection.WithProperty(keyName, value);
		}

		return doc;
	}

	private static ISidecarWriter ResolveWriter(SidecarFormat format) => format switch
	{
		SidecarFormat.Ini => new IniSidecarWriter(),
		SidecarFormat.Json => new JsonSidecarWriter(),
		_ => throw new InvalidOperationException($"Unsupported sidecar format: {format}"),
	};
}
