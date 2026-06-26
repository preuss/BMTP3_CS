using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Steps.HashStep;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.Steps;

public class HashItemStepTests
{
	private static HashItemStep BuildStep(HashStepContext context, IItemHasher hasher)
	{
		NullLogger<HashItemStep> logger = new();
		return new HashItemStep(context, hasher, logger);
	}

	// ── Tests ────────────────────────────────────────────────────────────────

	[Fact]
	public async Task ExecuteAsync_ComputesHashes_WhenItemHasNone()
	{
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[] { 1, 2, 3, 4, 5 });

			FileContent content = new(tempFile);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(tempFile));

			HashStepContext context = new() { HashTypes = new[] { HashType.SHA2_256 }, ForceRecompute = false };
			HashItemStep step = BuildStep(context, new Sha256ItemHasher());

			HashStepResult result = await step.ExecuteAsync(item, null!, CancellationToken.None);

			// Result dict must contain a SHA2_256 entry
			Assert.NotNull(result.Hashes);
			Assert.True(result.Hashes.ContainsKey(HashType.SHA2_256));
			Assert.False(string.IsNullOrWhiteSpace(result.Hashes[HashType.SHA2_256]));

			// Metadata must also be enriched
			Dictionary<HashType, string>? stored = item.Metadata.Get<Dictionary<HashType, string>>(MetadataKey.Hashes);
			Assert.NotNull(stored);
			Assert.True(stored!.ContainsKey(HashType.SHA2_256));
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task ExecuteAsync_SkipsRecompute_WhenHashesAlreadyPresent_AndForceRecomputeFalse()
	{
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[] { 10, 20, 30 });

			FileContent content = new(tempFile);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(tempFile));

			// Pre-set a known hash value
			const string presetHash = "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899";
			Dictionary<HashType, string> presetHashes = new() { { HashType.SHA2_256, presetHash } };
			item.Metadata.Set(MetadataKey.Hashes, presetHashes);

			// Track invocations – a real hasher that counts calls
			int callCount = 0;
			CountingHasher countingHasher = new(() => callCount++);

			HashStepContext context = new() { HashTypes = new[] { HashType.SHA2_256 }, ForceRecompute = false };
			HashItemStep step = BuildStep(context, countingHasher);

			HashStepResult result = await step.ExecuteAsync(item, null!, CancellationToken.None);

			// Hasher must NOT have been invoked because hash already existed
			Assert.Equal(0, callCount);
			// Original preset value must be preserved
			Assert.Equal(presetHash, result.Hashes[HashType.SHA2_256]);
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task ExecuteAsync_Recomputes_WhenForceRecomputeTrue()
	{
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[] { 99, 88, 77 });

			FileContent content = new(tempFile);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(tempFile));

			// Pre-set a bogus hash so we can detect it was replaced
			const string bogusHash = "0000000000000000000000000000000000000000000000000000000000000000";
			item.Metadata.Set(MetadataKey.Hashes,
				new Dictionary<HashType, string> { { HashType.SHA2_256, bogusHash } });

			HashStepContext context = new() { HashTypes = new[] { HashType.SHA2_256 }, ForceRecompute = true };
			HashItemStep step = BuildStep(context, new Sha256ItemHasher());

			HashStepResult result = await step.ExecuteAsync(item, null!, CancellationToken.None);

			// The bogus hash must have been replaced by the real one
			Assert.NotNull(result.Hashes);
			Assert.True(result.Hashes.ContainsKey(HashType.SHA2_256));
			Assert.NotEqual(bogusHash, result.Hashes[HashType.SHA2_256]);
			Assert.False(string.IsNullOrWhiteSpace(result.Hashes[HashType.SHA2_256]));
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch
			{
			}
		}
	}

	[Fact]
	public async Task ExecuteAsync_SetsItemFailed_AndRethrows_WhenHasherThrows()
	{
		// Use a non-existent file path so an exception is guaranteed when content is accessed.
		// The ThrowingItemHasher makes the failure deterministic regardless of file state.
		string tempFile = Path.GetTempFileName();
		try
		{
			await File.WriteAllBytesAsync(tempFile, new byte[] { 1 });

			FileContent content = new(tempFile);
			BackupItem item = BackupItem.Create(content, Path.GetFileName(tempFile));

			HashStepContext context = new() { HashTypes = new[] { HashType.SHA2_256 }, ForceRecompute = false };
			HashItemStep step = BuildStep(context, new ThrowingItemHasher());

			// HashItemStep re-throws after calling item.Fail()
			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				step.ExecuteAsync(item, null!, CancellationToken.None));

			// Verify the item was marked as failed
			Assert.Equal(ItemResultState.Failed, item.ResultState);
			Assert.True(item.Errors.HasErrors);
		}
		finally
		{
			try
			{
				File.Delete(tempFile);
			}
			catch
			{
			}
		}
	}
	// ── Helpers / fakes ──────────────────────────────────────────────────────

	/// <summary>
	///     Real SHA256-based IItemHasher that works on FileContent without any
	///     dependency on the full StreamHashGenerator / Blake3 native libs.
	/// </summary>
	private class Sha256ItemHasher : IItemHasher
	{
		public async Task<Dictionary<HashType, string>> ComputeHashesAsync(
			IBackupItem item,
			List<HashType> hashTypes,
			IProgress<ulong> progress,
			CancellationToken ct)
		{
			using SHA256 sha = SHA256.Create();
			await using Stream stream = await item.Content.OpenReadStreamAsync(ct);
			byte[] bytes = sha.ComputeHash(stream);
			string hex = Convert.ToHexString(bytes).ToLowerInvariant();

			Dictionary<HashType, string> result = new();
			foreach (HashType ht in hashTypes)
			{
				result[ht] = hex;
			}

			return result;
		}
	}

	/// <summary>
	///     IItemHasher that always throws to simulate a hashing failure.
	/// </summary>
	private class ThrowingItemHasher : IItemHasher
	{
		public Task<Dictionary<HashType, string>> ComputeHashesAsync(
			IBackupItem item,
			List<HashType> hashTypes,
			IProgress<ulong> progress,
			CancellationToken ct)
		{
			throw new InvalidOperationException("Simulated hasher failure");
		}
	}

	// ── Private helpers ──────────────────────────────────────────────────────

	private class CountingHasher : IItemHasher
	{
		private readonly Action _onCompute;

		public CountingHasher(Action onCompute)
		{
			_onCompute = onCompute;
		}

		public Task<Dictionary<HashType, string>> ComputeHashesAsync(
			IBackupItem item,
			List<HashType> hashTypes,
			IProgress<ulong> progress,
			CancellationToken ct)
		{
			_onCompute();
			// Return a valid but arbitrary hash so the step does not throw.
			Dictionary<HashType, string> result = new();
			foreach (HashType ht in hashTypes)
			{
				result[ht] = new string('a', 64);
			}

			return Task.FromResult(result);
		}
	}
}