using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.Sidecar;

public class IniSidecarGeneratorTests
{
	[Fact]
	public async Task GenerateAsync_WritesIniSidecarNextToFinalTargetPath()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_ini_test");
		Directory.CreateDirectory(dir);
		string src = Path.Combine(dir, "source.txt");
		string dest = Path.Combine(dir, "dest.txt");

		try
		{
			await File.WriteAllTextAsync(src, "dummy");
			await File.WriteAllTextAsync(dest, "dummy");

			BackupItem item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
			item.Metadata.Set(MetadataKey.FinalTargetPath, dest);
			item.Metadata.Set(MetadataKey.SourceFileName, Path.GetFileName(src));

			IniSidecarGenerator generator = new(new NullLogger<IniSidecarGenerator>());

			bool ok = await generator.GenerateAsync(item, CancellationToken.None);
			Assert.True(ok);

			string expected = Path.ChangeExtension(dest, ".ini");
			Assert.True(File.Exists(expected), "Expected .ini sidecar next to final target");

			string content = await File.ReadAllTextAsync(expected);
			Assert.Contains("final_target_path", content, StringComparison.OrdinalIgnoreCase); // metadata key present
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
				string expected = Path.ChangeExtension(dest, ".ini");
				if (File.Exists(expected))
				{
					File.Delete(expected);
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
	public async Task GenerateAsync_WritesHashes_AsTopLevelKeyValueLines()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_sidecar_ini_hashes_test");
		Directory.CreateDirectory(dir);
		string src = Path.Combine(dir, "source2.txt");
		string dest = Path.Combine(dir, "dest2.txt");

		try
		{
			await File.WriteAllTextAsync(src, "dummy2");
			await File.WriteAllTextAsync(dest, "dummy2");

			BackupItem item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
			item.Metadata.Set(MetadataKey.FinalTargetPath, dest);
			item.Metadata.Set(MetadataKey.Hashes, new Dictionary<HashType, string>
			{
				{ HashType.SHA2_256, "abc123" },
				{ HashType.BLAKE3_512, "def456" }
			});

			IniSidecarGenerator generator = new(new NullLogger<IniSidecarGenerator>());

			bool ok = await generator.GenerateAsync(item, CancellationToken.None);
			Assert.True(ok);

			string expected = Path.ChangeExtension(dest, ".ini");
			string content = await File.ReadAllTextAsync(expected);
			Assert.Contains("SHA2_256=abc123", content);
			Assert.Contains("BLAKE3_512=def456", content);
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
				string expected = Path.ChangeExtension(dest, ".ini");
				if (File.Exists(expected))
				{
					File.Delete(expected);
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