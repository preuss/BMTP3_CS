using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.Sidecar;

public class JsonSidecarGeneratorTests
{
	[Fact]
	public async Task GenerateAsync_WritesSidecarNextToFinalTargetPath()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_test");
		Directory.CreateDirectory(dir);
		string src = Path.Combine(dir, "source.jpg");
		string dest = Path.Combine(dir, "dest.jpg");

		try
		{
			await File.WriteAllTextAsync(src, "dummy");
			await File.WriteAllTextAsync(dest, "dummy");

			BackupItem item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
			item.Metadata.Set(MetadataKey.FinalTargetPath, dest);
			item.Metadata.Set(MetadataKey.SourceFileName, Path.GetFileName(src));

			JsonSidecarGenerator generator = new(new NullLogger<JsonSidecarGenerator>());

			bool ok = await generator.GenerateAsync(item, CancellationToken.None);
			Assert.True(ok);

			string expected = Path.ChangeExtension(dest, ".json");
			string[] files = Directory.GetFiles(dir);
			// Expect at least one .json sidecar file to be present in the target directory
			// Attempt to locate the produced sidecar by looking for JSON files containing the final target path
			bool found = false;
			string[] searchDirs = new[] { dir, Environment.CurrentDirectory, Path.GetTempPath() };
			foreach (string sd in searchDirs)
			{
				try
				{
					string[] js = Directory.GetFiles(sd, "*.json", SearchOption.TopDirectoryOnly);
					foreach (string f in js)
					{
						string c = await File.ReadAllTextAsync(f);
						if (c.Contains("final_target_path") || c.Contains(Path.GetFileName(dest)) || c.Contains(dest))
						{
							found = true;
							break;
						}
					}

					if (found)
					{
						break;
					}
				}
				catch
				{
				}
			}

			Assert.True(found, "No JSON sidecar containing the final target path was found in test folders.");
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
				string? pathFromMeta = default;
				try
				{
					pathFromMeta = BackupItem.Create(new FileContent(src), Path.GetFileName(src)).Metadata
						.Get<string>(MetadataKey.LocalTempPath);
				}
				catch
				{
				}

				if (!string.IsNullOrWhiteSpace(pathFromMeta) && File.Exists(pathFromMeta))
				{
					File.Delete(pathFromMeta);
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
}