using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.Tests.Strategies;

public class CollisionResolver_MismatchTests
{
	[Fact]
	public async Task ResolveAsync_WhenSidecarHashDiffers_ReturnsRename()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_cr_mismatch");
		Directory.CreateDirectory(dir);
		string dest = Path.Combine(dir, "file.jpg");
		string destSidecar = dest + ".bmtp3.json";
		string src = Path.Combine(dir, "src.jpg");
		try
		{
			byte[] srcData = new byte[2048];
			new Random(1).NextBytes(srcData);
			byte[] destData = new byte[2048];
			new Random(2).NextBytes(destData);
			await File.WriteAllBytesAsync(src, srcData);
			await File.WriteAllBytesAsync(dest, destData);

			// compute src hash
			string srcHex;
			using (SHA256 sha = SHA256.Create())
			using (FileStream s = File.OpenRead(src))
			{
				srcHex = string.Concat(sha.ComputeHash(s).Select(b => b.ToString("x2")));
			}

			// write dest sidecar with a different (fake) hash value
			string sidecarJson = "{\"hashes\": { \"SHA2_256\": \"deadbeef\" } }";
			await File.WriteAllTextAsync(destSidecar, sidecarJson);

			BackupItem item = BackupItem.Create(new FileContent(src), Path.GetFileName(src));
			item.Metadata.Set(MetadataKey.Hashes, new Dictionary<HashType, string> { { HashType.SHA2_256, srcHex } });

			BackupPlan plan = new()
				{ ComparisonType = CollisionComparisonType.Hash, CollisionResolution = CollisionResolutionType.Rename };

			CollisionResolver resolver = new(new DummyMetadataReader(), new SimplePathGenerator(),
				LoggerFactory.Create(b => { }).CreateLogger<CollisionResolver>());

			CollisionResult result = await resolver.ResolveAsync(item, dest, plan, CancellationToken.None);

			Assert.Equal(BackupActionType.Rename, result.Action);
		}
		finally
		{
			try
			{
				File.Delete(dest);
			}
			catch
			{
			}

			try
			{
				File.Delete(src);
			}
			catch
			{
			}

			try
			{
				File.Delete(destSidecar);
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

	private class SimplePathGenerator : IPathGenerator
	{
		public string ApplyPattern(string pattern, IBackupItem item)
		{
			return Path.GetFileName(item.SourcePath);
		}

		public string GenerateRelativePath(IBackupItem item, BackupPlan plan)
		{
			return Path.GetFileName(item.SourcePath);
		}
	}

	private class DummyMetadataReader : IMetadataReader
	{
		public Task EnrichMetadataAsync(IBackupItem item, CancellationToken ct)
		{
			return Task.CompletedTask;
		}
	}
}