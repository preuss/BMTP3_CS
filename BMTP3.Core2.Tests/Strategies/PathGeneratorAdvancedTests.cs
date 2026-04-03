using System;
using System.IO;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class PathGeneratorAdvancedTests
    {
        // Helper: creates a minimal BackupItem backed by a real temp file.
        private static BackupItem CreateItem(string fileName, string? relativePath = null)
        {
            string tempFile = Path.GetTempFileName();
            var content = new FileContent(tempFile);
            var item = BackupItem.Create(content, fileName, relativePath);
            return item;
        }

        // ──────────────────────────────────────────────────────────
        // PreserveSourceTree tests
        // ──────────────────────────────────────────────────────────

        [Fact]
        public void GenerateRelativePath_PreserveSourceTree_WithRelativePath_PreservesFolderHierarchy()
        {
            var pg = new PathGenerator();
            var plan = new BackupPlan { OutputStrategy = OutputStructureStrategy.PreserveSourceTree };

            var item = CreateItem("photo.jpg", @"DCIM\Camera");

            string result = pg.GenerateRelativePath(item, plan);

            Assert.Equal(Path.Combine(@"DCIM\Camera", "photo.jpg"), result);
        }

        [Fact]
        public void GenerateRelativePath_PreserveSourceTree_WhenRelativePathAbsent_ReturnsFileNameOnly()
        {
            var pg = new PathGenerator();
            var plan = new BackupPlan { OutputStrategy = OutputStructureStrategy.PreserveSourceTree };

            // No relativePath passed -> SourceRelativePath not set in metadata
            var item = CreateItem("photo.jpg");

            string result = pg.GenerateRelativePath(item, plan);

            Assert.Equal("photo.jpg", result);
        }

        // ──────────────────────────────────────────────────────────
        // CustomPathPattern – date/name token replacement
        // ──────────────────────────────────────────────────────────

        [Fact]
        public void ApplyPattern_YYYYToken_ReplacedWithFourDigitYearFromAuthoredDateTime()
        {
            var pg = new PathGenerator();
            var item = CreateItem("img001.jpg");
            var authored = new DateTime(2023, 7, 14, 10, 30, 0, DateTimeKind.Utc);
            item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);

            string result = pg.ApplyPattern("${YYYY}/photos", item);

            Assert.Equal("2023/photos", result);
        }

        [Fact]
        public void ApplyPattern_MMToken_ReplacedWithTwoDigitMonthFromAuthoredDateTime()
        {
            var pg = new PathGenerator();
            var item = CreateItem("img002.jpg");
            var authored = new DateTime(2023, 3, 5, 0, 0, 0, DateTimeKind.Utc);
            item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);

            string result = pg.ApplyPattern("${YYYY}/${MM}", item);

            Assert.Equal("2023/03", result);
        }

        [Fact]
        public void ApplyPattern_DDToken_ReplacedWithTwoDigitDayFromAuthoredDateTime()
        {
            var pg = new PathGenerator();
            var item = CreateItem("img003.jpg");
            var authored = new DateTime(2023, 3, 9, 0, 0, 0, DateTimeKind.Utc);
            item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);

            string result = pg.ApplyPattern("${YYYY}/${MM}/${DD}", item);

            Assert.Equal("2023/03/09", result);
        }

        [Fact]
        public void ApplyPattern_FilenameToken_ReplacedWithFileNameWithoutExtension()
        {
            var pg = new PathGenerator();
            var item = CreateItem("myphoto.jpg");
            var authored = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);

            string result = pg.ApplyPattern("${YYYY}/${filename}.jpg", item);

            Assert.Equal("2024/myphoto.jpg", result);
        }

        // ──────────────────────────────────────────────────────────
        // GetBestDate fallback behaviour (exercised via ApplyPattern)
        // ──────────────────────────────────────────────────────────

        [Fact]
        public void ApplyPattern_WhenAuthoredDateTimePresent_UsesAuthoredDateTime()
        {
            var pg = new PathGenerator();
            var item = CreateItem("img.jpg");

            var authored = new DateTime(2021, 11, 25, 0, 0, 0, DateTimeKind.Utc);
            var created  = new DateTime(2022, 5, 10, 0, 0, 0, DateTimeKind.Utc);

            item.Metadata.Set(MetadataKey.AuthoredDateTime, authored);
            item.Metadata.Set(MetadataKey.CreatedDateTime, created);

            // If AuthoredDateTime wins, year should be 2021, not 2022
            string result = pg.ApplyPattern("${YYYY}", item);

            Assert.Equal("2021", result);
        }

        [Fact]
        public void ApplyPattern_WhenAuthoredDateTimeAbsent_FallsBackToCreatedDateTime()
        {
            var pg = new PathGenerator();
            var item = CreateItem("img.jpg");

            var created = new DateTime(2020, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            item.Metadata.Set(MetadataKey.CreatedDateTime, created);
            // AuthoredDateTime deliberately NOT set

            string result = pg.ApplyPattern("${YYYY}", item);

            Assert.Equal("2020", result);
        }

        [Fact]
        public void ApplyPattern_WhenAuthoredAndCreatedAbsent_FallsBackToModifiedDateTime()
        {
            var pg = new PathGenerator();
            var item = CreateItem("img.jpg");

            var modified = new DateTime(2019, 4, 22, 0, 0, 0, DateTimeKind.Utc);
            item.Metadata.Set(MetadataKey.ModifiedDateTime, modified);
            // AuthoredDateTime and CreatedDateTime deliberately NOT set

            string result = pg.ApplyPattern("${YYYY}", item);

            Assert.Equal("2019", result);
        }
    }
}
