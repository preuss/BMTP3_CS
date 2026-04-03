using System;
using System.IO;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using BMTP3.Core2.BackupNew.Domain.Item;
using Xunit;

namespace BMTP3.Core2.Tests.Strategies
{
    public class PathGeneratorBehaviorTests
    {
        [Fact]
        public void GenerateRelativePath_FlatStrategy_UsesSourceFileName()
        {
            var pg = new PathGenerator();
            var plan = new BackupPlan { OutputStrategy = OutputStructureStrategy.Flat };

            var item = BackupItem.Create(new BMTP3.Core2.BackupNew.Content.FileContent(Path.GetTempFileName()), Path.GetFileName(Path.GetTempFileName()));
            item.Metadata.Set(BMTP3.Core2.BackupNew.Domain.Item.MetadataKey.SourceFileName, "testfile.jpg");

            string rel = pg.GenerateRelativePath(item, plan);

            Assert.Equal("testfile.jpg", rel);
        }
    }
}
