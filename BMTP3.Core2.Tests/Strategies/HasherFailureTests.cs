using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.Tests.Strategies;

public class HasherFailureTests
{
	[Fact]
	public async Task CollisionResolver_FallsBackToBinary_When_HashGenerator_Throws()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_hasherfail_cr");
		Directory.CreateDirectory(dir);
		string dest = Path.Combine(dir, "file.jpg");
		string src = Path.Combine(dir, "src.jpg");
		try
		{
			byte[] data = new byte[8192];
			new Random(1234).NextBytes(data);
			await File.WriteAllBytesAsync(dest, data);
			await File.WriteAllBytesAsync(src, data);

			// Compute SHA256 hex
			string shaHex;
			using (SHA256 sha = SHA256.Create())
			using (FileStream s = File.OpenRead(src))
			{
				byte[] h = sha.ComputeHash(s);
				shaHex = string.Concat(h.Select(b => b.ToString("x2")));
			}

			IContent content = new FileContent(src);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(src));
			Dictionary<HashType, string> hashes = new() { { HashType.SHA2_256, shaHex } };
			item.Metadata.Set(MetadataKey.Hashes, hashes);

			BackupPlan plan = new() { ComparisonType = CollisionComparisonType.Hash };

			CollisionResolver resolver = new(new DummyMetadataReader(), new SimplePathGenerator(),
				LoggerFactory.Create(b => { }).CreateLogger<CollisionResolver>(), new ThrowingHashGenerator());

			CollisionResult result = await resolver.ResolveAsync(item, dest, plan, CancellationToken.None);

			Assert.Equal(BackupActionType.Skip, result.Action);
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
				Directory.Delete(dir);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task DestinationInspector_DoesNotThrow_When_HashGenerator_Fails()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_hasherfail_di");
		Directory.CreateDirectory(dir);
		string dest = Path.Combine(dir, "file2.jpg");
		try
		{
			byte[] data = new byte[4096];
			new Random(4321).NextBytes(data);
			await File.WriteAllBytesAsync(dest, data);

			BackupPlan plan = new() { HashTypes = new HashSet<HashType> { HashType.SHA2_256 } };
			DestinationInspectorItemStep step = new(new ThrowingDestinationInspector(), new ThrowingHashGenerator(),
				plan, LoggerFactory.Create(b => { }).CreateLogger<DestinationInspectorItemStep>());

			FileContent content = new(dest);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(dest));
			item.Metadata.Set(MetadataKey.FinalTargetPath, dest);

			// Should not throw
			await step.ExecuteAsync(item, new Progress<ulong>(), CancellationToken.None);

			Dictionary<HashType, string>? stored =
				item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.DestinationHashes);
			Assert.True(stored == null || stored.Count == 0);
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

	private class ThrowingHashGenerator : IHashGenerator
	{
		public Task<Dictionary<HashType, string>> ComputeHashesAsync(Stream stream, IEnumerable<HashType> hashTypes,
			IProgress<ulong> progress, CancellationToken ct)
		{
			throw new InvalidOperationException("Simulated IHashGenerator failure");
		}
	}

	private class ThrowingDestinationInspector : IDestinationInspector
	{
		public Task<FileSnapshot> GetSnapshotAsync(string path, CancellationToken ct)
		{
			bool exists = File.Exists(path);
			ulong length = exists ? (ulong)new FileInfo(path).Length : 0UL;
			DateTime lastWrite = exists ? new FileInfo(path).LastWriteTimeUtc : default;
			return Task.FromResult(new FileSnapshot
			{
				Exists = exists,
				Length = length,
				LastWriteTimeUtc = lastWrite
			});
		}

		public Task<string> GetHashAsync(string path, string algorithm, CancellationToken ct)
		{
			throw new InvalidOperationException("Simulated IDestinationInspector.GetHashAsync failure");
		}
	}
}