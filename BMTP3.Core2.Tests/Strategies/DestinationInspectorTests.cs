using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using BMTP3.Core2.BackupNew.Engine.Steps.InspectorStep;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.Tests.Strategies;

public class DestinationInspectorTests
{
	[Fact]
	public async Task Inspector_Computes_And_Stores_DestinationHashes_When_No_Sidecar()
	{
		string dir = Path.Combine(Path.GetTempPath(), "bmtp3_destinsp");
		Directory.CreateDirectory(dir);
		string dest = Path.Combine(dir, "file.jpg");
		try
		{
			byte[] data = new byte[4096];
			new Random(42).NextBytes(data);
			await File.WriteAllBytesAsync(dest, data);

			BackupPlan plan = new();
			DestinationInspectorItemStep step = new(new FakeDestinationInspector(), new FakeHashGenerator(), plan,
				LoggerFactory.Create(b => { }).CreateLogger<DestinationInspectorItemStep>());

			FileContent content = new(dest);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(dest));
			item.Metadata.Set(MetadataKey.FinalTargetPath, dest);

			await step.ExecuteAsync(item, new Progress<ulong>(), CancellationToken.None);

			Dictionary<HashType, string>? stored =
				item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.DestinationHashes);
			Assert.NotNull(stored);
			Assert.True(stored.ContainsKey(HashType.SHA2_256) || stored.Count > 0);
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

	private class FakeHashGenerator : IHashGenerator
	{
		public async Task<Dictionary<HashType, string>> ComputeHashesAsync(Stream stream,
			IEnumerable<HashType> hashTypes, IProgress<ulong> progress, CancellationToken ct)
		{
			using SHA256 sha = SHA256.Create();
			byte[] h = sha.ComputeHash(stream);
			string hex = string.Concat(h.Select(b => b.ToString("x2")));
			Dictionary<HashType, string> dict = new();
			foreach (HashType ht in hashTypes)
			{
				dict[ht] = hex;
			}

			return await Task.FromResult(dict);
		}
	}

	private class FakeDestinationInspector : IDestinationInspector
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

		public async Task<string> GetHashAsync(string path, string algorithm, CancellationToken ct)
		{
			using SHA256 sha = SHA256.Create();
			using FileStream stream = File.OpenRead(path);
			byte[] h = sha.ComputeHash(stream);
			return await Task.FromResult(string.Concat(h.Select(b => b.ToString("x2"))));
		}
	}
}