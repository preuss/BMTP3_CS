using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;

namespace BMTP3.Core2.Tests.Strategies;

public class DefaultTimestampWaterfallTests
{
	// Helper: creates a minimal BackupItem backed by a real temp file.
	private static BackupItem CreateItem()
	{
		string tempFile = Path.GetTempFileName();
		FileContent content = new(tempFile);
		return BackupItem.Create(content, "test.jpg");
	}

	// ──────────────────────────────────────────────────────────
	// 1. EXIF (highest priority)
	// ──────────────────────────────────────────────────────────

	[Fact]
	public void Apply_WhenRawExifDateTakenPresent_SetsAuthoredDateTimeFromExif()
	{
		DefaultTimestampWaterfall waterfall = new();
		BackupItem item = CreateItem();

		DateTime exifDate = new(2022, 5, 10, 14, 30, 0, DateTimeKind.Utc);
		item.Metadata.Set(MetadataKey.RawExifDateTaken, exifDate);
		// Deliberately set lower-priority values to verify EXIF wins
		item.Metadata.Set(MetadataKey.RawMtpAuthoredDate, new DateTime(2020, 1, 1));
		item.Metadata.Set(MetadataKey.CreatedDateTime, new DateTime(2019, 1, 1));

		waterfall.Apply(item);

		Assert.Equal(exifDate, item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime));
		Assert.Equal(TimestampSource.Exif, item.Metadata.Get<TimestampSource>(MetadataKey.TimestampSource));
	}

	// ──────────────────────────────────────────────────────────
	// 2. MTP fallback
	// ──────────────────────────────────────────────────────────

	[Fact]
	public void Apply_WhenExifAbsentAndMtpPresent_SetsAuthoredDateTimeFromMtp()
	{
		DefaultTimestampWaterfall waterfall = new();
		BackupItem item = CreateItem();

		DateTime mtpDate = new(2021, 8, 20, 9, 0, 0, DateTimeKind.Utc);
		item.Metadata.Set(MetadataKey.RawMtpAuthoredDate, mtpDate);
		// CreatedDateTime present but should NOT win over MTP
		item.Metadata.Set(MetadataKey.CreatedDateTime, new DateTime(2018, 3, 3));

		waterfall.Apply(item);

		Assert.Equal(mtpDate, item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime));
		Assert.Equal(TimestampSource.Mtp, item.Metadata.Get<TimestampSource>(MetadataKey.TimestampSource));
	}

	// ──────────────────────────────────────────────────────────
	// 3. FileSystem Created fallback
	// ──────────────────────────────────────────────────────────

	[Fact]
	public void Apply_WhenExifAndMtpAbsentAndCreatedPresent_SetsAuthoredDateTimeFromCreated()
	{
		DefaultTimestampWaterfall waterfall = new();
		BackupItem item = CreateItem();

		DateTime createdDate = new(2017, 12, 1, 0, 0, 0, DateTimeKind.Utc);
		item.Metadata.Set(MetadataKey.CreatedDateTime, createdDate);
		// ModifiedDateTime present but should NOT win
		item.Metadata.Set(MetadataKey.ModifiedDateTime, new DateTime(2016, 6, 6));

		waterfall.Apply(item);

		Assert.Equal(createdDate, item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime));
		Assert.Equal(TimestampSource.FileSystem, item.Metadata.Get<TimestampSource>(MetadataKey.TimestampSource));
	}

	// ──────────────────────────────────────────────────────────
	// 4. FileSystem Modified fallback
	// ──────────────────────────────────────────────────────────

	[Fact]
	public void Apply_WhenExifMtpAndCreatedAbsent_SetsAuthoredDateTimeFromModified()
	{
		DefaultTimestampWaterfall waterfall = new();
		BackupItem item = CreateItem();

		DateTime modifiedDate = new(2015, 3, 28, 6, 0, 0, DateTimeKind.Utc);
		item.Metadata.Set(MetadataKey.ModifiedDateTime, modifiedDate);

		waterfall.Apply(item);

		Assert.Equal(modifiedDate, item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime));
		Assert.Equal(TimestampSource.LastModified, item.Metadata.Get<TimestampSource>(MetadataKey.TimestampSource));
	}

	// ──────────────────────────────────────────────────────────
	// 5. Ultimate fallback (UtcNow)
	// ──────────────────────────────────────────────────────────

	[Fact]
	public void Apply_WhenAllSourcesAbsent_SetsAuthoredDateTimeToNonNullFallback()
	{
		DefaultTimestampWaterfall waterfall = new();
		BackupItem item = CreateItem();
		// No metadata set – all sources absent

		DateTime before = DateTime.UtcNow.AddSeconds(-1);
		waterfall.Apply(item);
		DateTime after = DateTime.UtcNow.AddSeconds(1);

		DateTime authored = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);

		Assert.True(authored >= before && authored <= after,
			$"Expected fallback to UtcNow but got {authored}");
		Assert.Equal(TimestampSource.Unknown, item.Metadata.Get<TimestampSource>(MetadataKey.TimestampSource));
	}
}