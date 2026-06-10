using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Exceptions;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;
using BMTP3.Core4.Tests.Fakes;

namespace BMTP3.Core4.Tests.Engine.Hashing;

public class HashServiceTests
{
	private static readonly HashService Service = new(new FakeHashGenerator());

	[Fact]
	public async Task ComputeHashesAsync_SingleAlgorithm_ReturnsExpectedHash()
	{
		var content = new FakeContent("hello world");

		Dictionary<HashType, string> result = await Service.ComputeHashesAsync(
			content, "test.txt", new[] { HashAlgorithmType.SHA2_256 }, null, default);

		Assert.Single(result);
		Assert.True(result.ContainsKey(HashType.SHA2_256));
		Assert.Equal("fake-SHA2_256", result[HashType.SHA2_256]);
	}

	[Fact]
	public async Task ComputeHashesAsync_MultipleAlgorithms_ReturnsAllRequested()
	{
		var content = new FakeContent("test data");

		Dictionary<HashType, string> result = await Service.ComputeHashesAsync(
			content, "test.txt",
			new[] { HashAlgorithmType.SHA2_256, HashAlgorithmType.MD5_128, HashAlgorithmType.BLAKE3_256 },
			null, default);

		Assert.Equal(3, result.Count);
		Assert.True(result.ContainsKey(HashType.SHA2_256));
		Assert.True(result.ContainsKey(HashType.MD5_128));
		Assert.True(result.ContainsKey(HashType.BLAKE3_256));
	}

	[Fact]
	public async Task ComputeHashesAsync_EmptyAlgorithms_ReturnsEmptyDict()
	{
		var content = new FakeContent("anything");

		Dictionary<HashType, string> result = await Service.ComputeHashesAsync(
			content, "test.txt", Array.Empty<HashAlgorithmType>(), null, default);

		Assert.Empty(result);
	}

	[Fact]
	public async Task ComputeHashesAsync_Cancellation_Propagates()
	{
		var content = new FakeContent("data");
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			Service.ComputeHashesAsync(content, "test.txt",
				new[] { HashAlgorithmType.SHA2_256 }, null, cts.Token));
	}

	[Fact]
	public async Task ComputeHashesAsync_ContentThrows_WrapsInBackupHashException()
	{
		var failingContent = new FailingContent();
		HashService localService = new(new FakeHashGenerator());

		BackupHashException ex = await Assert.ThrowsAsync<BackupHashException>(() =>
			localService.ComputeHashesAsync(failingContent, "failing.txt",
				new[] { HashAlgorithmType.SHA2_256 }, null, default));

		Assert.Equal("failing.txt", ex.ItemRelativeFilePath);
	}

	private sealed class FailingContent : IContent
	{
		public ulong Length => 10;
		public void Dispose() { }
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
		public Stream OpenRead() => throw new IOException("disk error");
		public Task<Stream> OpenReadAsync(CancellationToken ct) =>
			throw new IOException("disk error");
	}
}
