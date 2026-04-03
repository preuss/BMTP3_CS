using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using System.Text.RegularExpressions;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Standard implementation of IPathGenerator.
/// Handles Flat, PreserveSourceTree and CustomPathPattern strategies.
/// </summary>
public class PathGenerator : IPathGenerator
{
	// Regex matches tokens like ${YYYY}, ${Model}, etc.
	private static readonly Regex TokenRegex = new(@"\$\{?([a-zA-Z0-9_]+)\}?", RegexOptions.Compiled);

	public string GenerateRelativePath(IBackupItem item, BackupPlan plan)
	{
		string fileName = item.Metadata.Get<string>(MetadataKey.SourceFileName)
						  ?? throw new InvalidOperationException("SourceFileName is missing in metadata.");

		switch(plan.OutputStrategy)
		{
			case OutputStructureStrategy.Flat:
				return fileName;

			case OutputStructureStrategy.PreserveSourceTree:
				var relPath = item.Metadata.Get<string>(MetadataKey.SourceRelativePath);

				if(string.IsNullOrWhiteSpace(relPath) || relPath.Trim(Path.DirectorySeparatorChar) == "")
				{
					return fileName;
				}

				return Path.Combine(relPath, fileName);

			case OutputStructureStrategy.CustomPathPattern:
				if(string.IsNullOrWhiteSpace(plan.CustomOutputPathPattern))
				{
					return fileName;
				}
				return ApplyPattern(plan.CustomOutputPathPattern, item);

			default:
				return fileName;
		}
	}

	public string ApplyPattern(string pattern, IBackupItem item)
	{
		if(string.IsNullOrWhiteSpace(pattern)) return string.Empty;

		string originalFileName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? "unknown";
		return TokenRegex.Replace(pattern, match =>
		{
			string key = match.Groups[1].Value;
			return GetTokenValueStrict(key, item, originalFileName);
		});
	}

	private string GetTokenValueStrict(string key, IBackupItem item, string originalFileName)
	{
		DateTime date = GetBestDate(item);

		switch(key)
		{
			// --- Date & Time ---
			case "YYYY": return date.ToString("yyyy"); // Spec: 2025
			case "MM": return date.ToString("MM");   // Spec: 01-12
			case "DD": return date.ToString("dd");   // Spec: 01-31
			case "hh": return date.ToString("HH");   // Spec: 00-23 (Important: C# 'hh' is 12h, 'HH' is 24h)
			case "mm": return date.ToString("mm");   // Spec: 00-59
			case "ss": return date.ToString("ss");   // Spec: 00-59

			case "SSS":
			case "fff":
				return date.ToString("fff");           // Spec: Milliseconds (3 digits)

			case "ffffff": return date.ToString("ffffff");    // Spec: Microseconds
			case "fffffffff": return date.ToString("fffffff00");   // Spec: Nanoseconds

			// --- Metadata ---
			case "Model":
			case "model":
				return item.Metadata.Get<string>(MetadataKey.Model)
					   ?? item.Metadata.Get<string>(MetadataKey.DeviceName)
					   ?? "UnknownModel";

			case "deviceName":
			case "devicename":
				return item.Metadata.Get<string>(MetadataKey.DeviceName) ?? "UnknownDevice";

			case "OriginalFileName":
			case "originalName":
			case "filename":
				return Path.GetFileNameWithoutExtension(originalFileName);

			case "originalFullName":
				return originalFileName;

			case "ext":
			case "extension":
				return Path.GetExtension(originalFileName).TrimStart('.');

			case "SourceRelativePath":
			case "sourceRelativePath":
			case "relativePath":



				return item.Metadata.Get<string>(MetadataKey.SourceRelativePath) ?? "";

			case "sourceId":
				return item.Metadata.Get<string>(MetadataKey.SourceId) ?? "UnknownSource"; // Mapping SourceId

			case "count":
				// Count is usually handled by CollisionResolver, but if requested in path, return placeholder or 1.
				// Since this is generation *before* collision check, this might be ambiguous.
				// However, spec lists it. Returning "1" as default for initial generation.
                // If no collision index has been set yet, return "1" as a conservative default.
                // This ensures templates containing ${count} produce a stable output before collision resolution.
                // CollisionResolver will update the actual final name when resolving.
                return item.Metadata.Get<string>(MetadataKey.CollisionIndex) ?? "1";

			// --- Hashes ---
			case "hashShort": return GetHash(item, 6);
			case "hashMedium": return GetHash(item, 12);
			case "hashLong": return GetHash(item, 0);

			default:
				// We throw an exception to fail the path generation for this item.
				throw new ArgumentException($"Invalid template variable '${key}'. Variable names are case-sensitive (e.g., use ${{YYYY}}, not ${{yyyy}}).");
		}
	}

	private string GetHash(IBackupItem item, int length)
	{
		if(!item.Metadata.Has(MetadataKey.Hashes)) return "nohash";

		var hashes = item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.Hashes);
		if(hashes == null || hashes.Count == 0) return "nohash";

		// Prefer strong hashes
		string hash = "";
		if(hashes.ContainsKey(HashType.BLAKE3_512)) hash = hashes[HashType.BLAKE3_512];
		else if(hashes.ContainsKey(HashType.SHA2_256)) hash = hashes[HashType.SHA2_256];
		else if(hashes.ContainsKey(HashType.MD5_128)) hash = hashes[HashType.MD5_128];
		else hash = hashes.Values.FirstOrDefault() ?? "nohash";

		if(length > 0 && hash.Length > length)
			return hash.Substring(0, length);

		return hash;
	}
	private DateTime GetBestDate(IBackupItem item)
	{
		if(item.Metadata.Has(MetadataKey.AuthoredDateTime))
			return item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);

		if(item.Metadata.Has(MetadataKey.CreatedDateTime))
			return item.Metadata.Get<DateTime>(MetadataKey.CreatedDateTime);

		if(item.Metadata.Has(MetadataKey.ModifiedDateTime))
			return item.Metadata.Get<DateTime>(MetadataKey.ModifiedDateTime);

		return DateTime.UtcNow;
	}
}
