namespace BMTP3.Core2.BackupNew.Models;
/// <summary>
/// Alle kendte metadata-nøgler.
/// Brug af enum + attribute giver både typesikkerhed og mulighed for at serialisere til læsbare strings.
/// </summary>
public enum MetadataKey
{
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
/// Attribute that makes it possible to map the enum value to a readable string during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class MetadataKeyInfoAttribute : Attribute
{
	public string Key { get; }
	public MetadataKeyInfoAttribute(string key) => Key = key;
}

