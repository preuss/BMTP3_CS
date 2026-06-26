using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Steps.SidecarGenerationStep;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.Sidecar;

public class SidecarGenerationStepTests
{
	[Fact]
	public async Task SidecarGeneration_WritesIni_WhenPlanSpecifiesIni()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_step_ini");
		Directory.CreateDirectory(dir);
		string src = Path.Combine(dir, "s.txt");
		string dest = Path.Combine(dir, "d.txt");

		try
		{
			await File.WriteAllTextAsync(src, "x");
			await File.WriteAllTextAsync(dest, "x");

			BackupPlan plan = new() { SidecarFormat = SidecarFormat.Ini };
			BackupItem item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
			item.Metadata.Set(MetadataKey.FinalTargetPath, dest);

			SidecarGenerationItemStep step = new(plan, new TestFactory());
			bool ok = await step.ExecuteAsync(item, null!, CancellationToken.None);
			Assert.True(ok);

			string expected = Path.ChangeExtension(dest, ".ini");
			Assert.True(File.Exists(expected), "Expected .ini sidecar was not written");
		}
		finally
		{
			try
			{
				File.Delete(src);
			}
			catch
			{
			}

			try
			{
				File.Delete(dest);
			}
			catch
			{
			}

			try
			{
				string p = Path.ChangeExtension(dest, ".ini");
				if (File.Exists(p))
				{
					File.Delete(p);
				}
			}
			catch
			{
			}

			try
			{
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task SidecarGeneration_WritesJson_WhenPlanSpecifiesJson()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_step_json");
		Directory.CreateDirectory(dir);
		string src = Path.Combine(dir, "s2.txt");
		string dest = Path.Combine(dir, "d2.txt");

		try
		{
			await File.WriteAllTextAsync(src, "x");
			await File.WriteAllTextAsync(dest, "x");

			BackupPlan plan = new() { SidecarFormat = SidecarFormat.Json };
			BackupItem item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
			item.Metadata.Set(MetadataKey.FinalTargetPath, dest);

			SidecarGenerationItemStep step = new(plan, new TestFactory());
			bool ok = await step.ExecuteAsync(item, null!, CancellationToken.None);
			Assert.True(ok);

			string expected = dest + ".bmtp3.json";
			Assert.True(File.Exists(expected), "Expected .json sidecar was not written");
		}
		finally
		{
			try
			{
				File.Delete(src);
			}
			catch
			{
			}

			try
			{
				File.Delete(dest);
			}
			catch
			{
			}

			try
			{
				string p = dest + ".bmtp3.json";
				if (File.Exists(p))
				{
					File.Delete(p);
				}
			}
			catch
			{
			}

			try
			{
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}

	private class TestFactory : ISidecarGeneratorFactory
	{
		public ISidecarGenerator? Create(SidecarFormat format)
		{
			return format switch
			{
				SidecarFormat.Ini => new IniSidecarGenerator(new NullLogger<IniSidecarGenerator>()),
				SidecarFormat.Json => new JsonSidecarGenerator(new NullLogger<JsonSidecarGenerator>()),
				_ => null
			};
		}
	}
}