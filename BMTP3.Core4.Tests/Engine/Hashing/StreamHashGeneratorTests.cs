using System.Security.Cryptography;
using System.Text;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Hashing.Crypto;
using BMTP3.Core4.Infrastructure.Throttling;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core4.Tests.Engine.Hashing;

public class StreamHashGeneratorTests
{
	private static readonly byte[] TestData = Encoding.UTF8.GetBytes("hello world");
	private static readonly StreamHashGenerator Generator = new(NullLogger<StreamHashGenerator>.Instance);

	[Fact]
	public async Task ComputeHashesAsync_AllNineAlgorithms_ReturnsExpectedHashes()
	{
		await using MemoryStream stream = new(TestData);

		Dictionary<HashType, string> results = await Generator.ComputeHashesAsync(
			stream,
			Enum.GetValues<HashType>(),
			null,
			new NoOpThrottler(),
			default);

		Assert.Equal(9, results.Count);

		Assert.Equal(ComputeHex(SHA256.Create(), TestData), results[HashType.SHA2_256]);
		Assert.Equal(ComputeHex(SHA512.Create(), TestData), results[HashType.SHA2_512]);
		Assert.Equal(ComputeHex(MD5.Create(), TestData), results[HashType.MD5_128]);

		Assert.Equal(ComputeHex(new SharpHashSHA3_256(), TestData), results[HashType.SHA3_256_FIPS202]);
		Assert.Equal(ComputeHex(new SharpHashSHA3_512(), TestData), results[HashType.SHA3_512_FIPS202]);
		Assert.Equal(ComputeHex(new SharpHashSHA3_256_Keccak(), TestData), results[HashType.SHA3_256_KECCAK]);
		Assert.Equal(ComputeHex(new SharpHashSHA3_512_Keccak(), TestData), results[HashType.SHA3_512_KECCAK]);

		Assert.Equal(ComputeHex(new Blake3HashAlgorithm(32), TestData), results[HashType.BLAKE3_256]);
		Assert.Equal(ComputeHex(new Blake3HashAlgorithm(64), TestData), results[HashType.BLAKE3_512]);
	}

	[Fact]
	public async Task ComputeHashesAsync_SingleAlgorithm_ReturnsOneResult()
	{
		await using MemoryStream stream = new(TestData);

		Dictionary<HashType, string> results = await Generator.ComputeHashesAsync(
			stream,
			[HashType.BLAKE3_512],
			null,
			new NoOpThrottler(),
			default);

		Assert.Single(results);
		Assert.True(results.ContainsKey(HashType.BLAKE3_512));
		Assert.Equal(ComputeHex(new Blake3HashAlgorithm(64), TestData), results[HashType.BLAKE3_512]);
	}

	[Fact]
	public async Task ComputeHashesAsync_EmptyStream_ReturnsZeroLengthHashes()
	{
		await using MemoryStream stream = new([]);

		Dictionary<HashType, string> results = await Generator.ComputeHashesAsync(
			stream,
			[HashType.SHA2_256, HashType.MD5_128],
			null,
			new NoOpThrottler(),
			default);

		Assert.Equal(2, results.Count);
		Assert.Equal(ComputeHex(SHA256.Create(), []), results[HashType.SHA2_256]);
		Assert.Equal(ComputeHex(MD5.Create(), []), results[HashType.MD5_128]);
	}

	[Fact]
	public async Task ComputeHashesAsync_EmptyAlgorithms_ReturnsEmpty()
	{
		await using MemoryStream stream = new(TestData);

		Dictionary<HashType, string> results = await Generator.ComputeHashesAsync(
			stream,
			Array.Empty<HashType>(),
			null,
			new NoOpThrottler(),
			default);

		Assert.Empty(results);
	}

	[Fact]
	public async Task ComputeHashesAsync_LargeData_AllAlgorithmsMatch()
	{
		byte[] largeData = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. ", 1000)));
		await using MemoryStream stream = new(largeData);

		Dictionary<HashType, string> results = await Generator.ComputeHashesAsync(
			stream,
			Enum.GetValues<HashType>(),
			null,
			new NoOpThrottler(),
			default);

		Assert.Equal(9, results.Count);
		Assert.Equal(ComputeHex(SHA256.Create(), largeData), results[HashType.SHA2_256]);
		Assert.Equal(ComputeHex(SHA512.Create(), largeData), results[HashType.SHA2_512]);
	}

	[Fact]
	public async Task ComputeHashesAsync_Cancellation_Propagates()
	{
		await using MemoryStream stream = new(TestData);
		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			Generator.ComputeHashesAsync(stream, [HashType.SHA2_256], null, new NoOpThrottler(), cts.Token));
	}

	[Fact]
	public async Task ComputeHashesAsync_DuplicateAlgorithms_ReturnsDistinct()
	{
		await using MemoryStream stream = new(TestData);

		Dictionary<HashType, string> results = await Generator.ComputeHashesAsync(
			stream,
			[HashType.SHA2_256, HashType.SHA2_256, HashType.SHA2_256],
			null,
			new NoOpThrottler(),
			default);

		Assert.Single(results);
	}

	private static string ComputeHex(HashAlgorithm algo, byte[] data)
	{
		using(algo)
		{
			return Convert.ToHexString(algo.ComputeHash(data)).ToLowerInvariant();
		}
	}
}
