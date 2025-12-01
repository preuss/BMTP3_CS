using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// Alle kendte metadata-nøgler.
/// Brug af enum + attribute giver både typesikkerhed og mulighed for at serialisere til læsbare strings.
/// </summary>
public enum MetadataKey {
	// ── Identifikation ─────────────────────────────────────
	[MetadataKeyInfo("original_source_id")]
	OriginalSourceId,               // MTP PersistentUniqueId eller FileInfo.FullName

	[MetadataKeyInfo("original_file_name")]
	OriginalFileName,

	[MetadataKeyInfo("source_relative_path")]
	SourceRelativePath,             // F.eks. "DCIM\100APPLE\" på telefonen

	// ── Grundlæggende attributter ──────────────────────────
	[MetadataKeyInfo("size")]
	Size,                           // long – gemmes også i ISourceContent

	// ── Tidsstempler fra kilden ───────────────────────────
	[MetadataKeyInfo("datetime_authored")]
	AuthoredDateTime,               // EXIF DateTimeOriginal eller lign.

	[MetadataKeyInfo("datetime_modified")]
	ModifiedDateTime,

	[MetadataKeyInfo("datetime_created")]
	CreatedDateTime,

	// ── Resultater fra stages (berigelse) ──────────────────
	[MetadataKeyInfo("local_temp_path")]
	LocalTempPath,                  // string – fuld sti til midlertidig fil

	[MetadataKeyInfo("hash_sha256")]
	HashSha256,                     // string (hex)

	[MetadataKeyInfo("final_target_path")]
	FinalTargetPath,                // string – hvor filen ender i backup-mappen

	// ── Fremtidige (eksempler du kan tilføje senere) ───────
	// [MetadataKeyInfo("is_duplicate")]
	// IsDuplicate,
	// [MetadataKeyInfo("duplicate_of_path")]
	// DuplicateOfPath,
}

/// <summary>
/// Attribute der gør det muligt at mappe enum-værdien til en læsbar string ved serialisering.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class MetadataKeyInfoAttribute : Attribute {
	public string Key { get; }
	public MetadataKeyInfoAttribute(string key) => Key = key;
}

// Ekstra hjælpemetode – gør det nemt at få string-repræsentationen
public static class MetadataKeyExtensions {
	public static string GetKey(this MetadataKey key) {
		var field = key.GetType().GetField(key.ToString())
					?? throw new ArgumentException($"No field found for {key}");

		var attribute = field.GetCustomAttributes(typeof(MetadataKeyInfoAttribute), false)
							 .Cast<MetadataKeyInfoAttribute>()
							 .FirstOrDefault();

		return attribute?.Key ?? key.ToString();
	}
}