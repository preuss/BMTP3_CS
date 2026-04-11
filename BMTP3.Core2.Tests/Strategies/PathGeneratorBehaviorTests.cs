using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;

namespace BMTP3.Core2.Tests.Strategies;

public class PathGeneratorBehaviorTests
{
	[Fact]
	public void GenerateRelativePath_FlatStrategy_UsesSourceFileName()
	{
		PathGenerator pg = new();
		BackupPlan plan = new() { OutputStrategy = OutputStructureStrategy.Flat };

		BackupItem item = BackupItem.Create(new FileContent(Path.GetTempFileName()),
			Path.GetFileName(Path.GetTempFileName()));
		item.Metadata.Set(MetadataKey.SourceFileName, "testfile.jpg");

		string rel = pg.GenerateRelativePath(item, plan);

		Assert.Equal("testfile.jpg", rel);
	}
}