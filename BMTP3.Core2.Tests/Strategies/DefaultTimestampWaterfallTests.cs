using System;
using System.IO;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class DefaultTimestampWaterfallTests
    {
        // Helper: creates a minimal BackupItem backed by a real temp file.
        private static BackupItem CreateItem()
        {
            string tempFile = Path.GetTempFileName();
            var content = new FileContent(tempFile);
            return BackupItem.Create(content, "test.jpg");
        }

        // ──────────────────────────────────────────────────────────
        // 1. EXIF (highest priority)
        // ──────────────────────────────────────────────────────────

        [Fact]
        public void Apply_WhenRawExifDateTakenPresent_SetsAuthoredDateTimeFromExif()
        {
            var waterfall = new DefaultTimestampWaterfall();
            var item = CreateItem();

            var exifDate = new DateTime(2022, 5, 10, 14, 30, 0, DateTimeKind.Utc);
            item.Metadata.Set(MetadataKey.RawExifDateTaken, exifDate);
            // Deliberately set lower-priority values to verify EXIF wins
            item.Metadata.Set(MetadataKey.RawMtpAuthoredDate, new DateTime(2020, 1, 1));
            item.Metadata.Set(MetadataKey.CreatedDateTime,    new DateTime(2019, 1, 1));

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
            var waterfall = new DefaultTimestampWaterfall();
            var item = CreateItem();

            var mtpDate = new DateTime(2021, 8, 20, 9, 0, 0, DateTimeKind.Utc);
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
            var waterfall = new DefaultTimestampWaterfall();
            var item = CreateItem();

            var createdDate = new DateTime(2017, 12, 1, 0, 0, 0, DateTimeKind.Utc);
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
            var waterfall = new DefaultTimestampWaterfall();
            var item = CreateItem();

            var modifiedDate = new DateTime(2015, 3, 28, 6, 0, 0, DateTimeKind.Utc);
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
            var waterfall = new DefaultTimestampWaterfall();
            var item = CreateItem();
            // No metadata set – all sources absent

            var before = DateTime.UtcNow.AddSeconds(-1);
            waterfall.Apply(item);
            var after  = DateTime.UtcNow.AddSeconds(1);

            var authored = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);

            Assert.True(authored >= before && authored <= after,
                $"Expected fallback to UtcNow but got {authored}");
            Assert.Equal(TimestampSource.Unknown, item.Metadata.Get<TimestampSource>(MetadataKey.TimestampSource));
        }
    }
}
