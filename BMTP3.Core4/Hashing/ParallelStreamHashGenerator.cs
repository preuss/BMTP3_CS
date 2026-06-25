using BMTP3.Core4.Hashing.Crypto;
using BMTP3.Core4.Infrastructure.Throttling;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Security.Cryptography;

namespace BMTP3.Core4.Hashing;

public sealed class ParallelStreamHashGenerator : IHashGenerator
{
	//private const int DefaultBufferSize = 4 * 1024 * 1024; // 4 MB buffer for large sequential stream hashing.
	private const int DefaultBufferSize = 8 * 1024 * 1024; // 8 MB buffer for large sequential stream hashing.

	private readonly ILogger<ParallelStreamHashGenerator> _logger;
	private readonly int _bufferSize;
	private readonly int _maxDegreeOfParallelism;

	public ParallelStreamHashGenerator(ILogger<ParallelStreamHashGenerator> logger)
		: this(DefaultBufferSize, Math.Max(2, Environment.ProcessorCount / 3), logger)
	{
	}

	public ParallelStreamHashGenerator(int bufferSize, int maxDegreeOfParallelism, ILogger<ParallelStreamHashGenerator> logger)
	{
		if(bufferSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be greater than zero.");

		if(maxDegreeOfParallelism <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Max degree of parallelism must be greater than zero.");

		_bufferSize = bufferSize;
		_maxDegreeOfParallelism = maxDegreeOfParallelism;
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(Stream stream, IEnumerable<HashType> hashTypes, IProgress<ulong>? progress, IThrottler throttler, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(hashTypes);
		ArgumentNullException.ThrowIfNull(throttler);

		if(!stream.CanRead)
			throw new ArgumentException("Stream must be readable.", nameof(stream));

		ct.ThrowIfCancellationRequested();

		List<HashType> requested = hashTypes.Distinct().ToList();

		if(requested.Count == 0)
		{
			return new Dictionary<HashType, string>();
		}

		Dictionary<HashType, HashAlgorithm> algorithms = new(requested.Count);
		byte[]? buffer = null;

		try
		{
			foreach(HashType type in requested)
			{
				algorithms[type] = CreateAlgorithm(type);
			}

			KeyValuePair<HashType, HashAlgorithm>[] algorithmEntries = algorithms.ToArray();

			ParallelOptions parallelOptions = new()
			{
				CancellationToken = ct,
				MaxDegreeOfParallelism = Math.Min(_maxDegreeOfParallelism, algorithmEntries.Length)
			};

			_logger.LogDebug(
				"Computing {HashCount} hashes with buffer size {BufferSize} and max parallelism {MaxDegreeOfParallelism}.",
				algorithmEntries.Length,
				_bufferSize,
				parallelOptions.MaxDegreeOfParallelism);

			buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);

			ulong totalBytesRead = 0;

			while(true)
			{
				int bytesRead = await stream
					.ReadAsync(buffer.AsMemory(0, _bufferSize), ct)
					.ConfigureAwait(false);

				if(bytesRead == 0)
					break;

				totalBytesRead += (ulong)bytesRead;

				// Each hash algorithm receives the same chunk.
				// Each individual algorithm still processes chunks sequentially,
				// but different algorithms run in parallel.
				Parallel.ForEach(
					algorithmEntries,
					parallelOptions,
					entry =>
					{
						entry.Value.TransformBlock(buffer, 0, bytesRead, null, 0);
					});

				progress?.Report(totalBytesRead);

				// Optional throttling.
				await throttler.WaitAsync().ConfigureAwait(false);
			}

			ct.ThrowIfCancellationRequested();

			Dictionary<HashType, string> results = new(algorithms.Count);

			foreach(KeyValuePair<HashType, HashAlgorithm> kvp in algorithms)
			{
				kvp.Value.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

				byte[] hashBytes = kvp.Value.Hash
					?? throw new CryptographicException($"Hash algorithm did not produce a hash: {kvp.Key}");

				results[kvp.Key] = Convert.ToHexString(hashBytes).ToLowerInvariant();
			}

			return results;
		} finally
		{
			if(buffer != null)
			{
				ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
			}

			foreach(HashAlgorithm algo in algorithms.Values)
			{
				algo.Dispose();
			}
		}
	}

	private static HashAlgorithm CreateAlgorithm(HashType type)
	{
		return type switch
		{
			HashType.MD5_128 => MD5.Create(),
			HashType.SHA2_256 => SHA256.Create(),
			HashType.SHA2_512 => SHA512.Create(),

			HashType.SHA3_256_FIPS202 => new SharpHashSHA3_256(),
			HashType.SHA3_512_FIPS202 => new SharpHashSHA3_512(),
			HashType.SHA3_256_KECCAK => new SharpHashSHA3_256_Keccak(),
			HashType.SHA3_512_KECCAK => new SharpHashSHA3_512_Keccak(),

			HashType.BLAKE3_256 => new Blake3HashAlgorithm(32),
			HashType.BLAKE3_512 => new Blake3HashAlgorithm(64),

			_ => throw new NotSupportedException($"HashType {type} is not supported.")
		};
	}
}