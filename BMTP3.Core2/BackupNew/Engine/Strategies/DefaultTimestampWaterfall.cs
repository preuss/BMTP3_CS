using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
///     Default implementation of the timestamp waterfall:
///     EXIF &gt; MTP &gt; FileSystem Created &gt; FileSystem Modified &gt; UTC Now.
/// </summary>
public class DefaultTimestampWaterfall : ITimestampWaterfall
{
	public void Apply(IBackupItem item)
	{
		ArgumentNullException.ThrowIfNull(item);

		// 1. EXIF (Highest Priority)
		if (item.Metadata.Has(MetadataKey.RawExifDateTaken))
		{
			DateTime exifDate = item.Metadata.Get<DateTime>(MetadataKey.RawExifDateTaken);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, exifDate);
			item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.Exif);
			item.AddLog($"Timestamp set from EXIF: {exifDate}", "TimestampCorrection");
			return;
		}

		// 2. MTP (Medium Priority)
		if (item.Metadata.Has(MetadataKey.RawMtpAuthoredDate))
		{
			DateTime mtpDate = item.Metadata.Get<DateTime>(MetadataKey.RawMtpAuthoredDate);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, mtpDate);
			item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.Mtp);
			item.AddLog($"Timestamp set from MTP: {mtpDate}", "TimestampCorrection");
			return;
		}

		// 3. FileSystem Created (Low Priority)
		if (item.Metadata.Has(MetadataKey.CreatedDateTime))
		{
			DateTime created = item.Metadata.Get<DateTime>(MetadataKey.CreatedDateTime);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, created);
			item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.FileSystem);
			item.AddLog($"Timestamp set from FS Created: {created}", "TimestampCorrection");
			return;
		}

		// 4. FileSystem Modified (Lowest Priority)
		if (item.Metadata.Has(MetadataKey.ModifiedDateTime))
		{
			DateTime mod = item.Metadata.Get<DateTime>(MetadataKey.ModifiedDateTime);
			item.Metadata.Set(MetadataKey.AuthoredDateTime, mod);
			item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.LastModified);
			item.AddLog($"Timestamp set from FS Modified: {mod}", "TimestampCorrection");
			return;
		}

		// 5. Fallback
		DateTime now = DateTime.UtcNow;
		item.Metadata.Set(MetadataKey.AuthoredDateTime, now);
		item.Metadata.Set(MetadataKey.TimestampSource, TimestampSource.Unknown);
		item.AddLog("Timestamp fallback to UTC Now", "TimestampCorrection");
	}
}