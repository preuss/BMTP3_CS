using BMTP3.Core4.Hashing.Crypto;
using BMTP3.Core4.Infrastructure.Throttling;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Security.Cryptography;

namespace BMTP3.Core4.Hashing;

public sealed class PooledStreamHashGenerator : IHashGenerator
{
	// 4 MB buffer for large sequential stream hashing.
	// Uses ArrayPool to avoid repeated large allocations.
	private const int DefaultBufferSize = 4 * 1024 * 1024;

	private readonly ILogger<PooledStreamHashGenerator> _logger;
	private readonly int _bufferSize;

	public PooledStreamHashGenerator(ILogger<PooledStreamHashGenerator> logger)
		: this(DefaultBufferSize, logger)
	{
	}

	public PooledStreamHashGenerator(int bufferSize, ILogger<PooledStreamHashGenerator> logger)
	{
		if(bufferSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be greater than zero.");

		_bufferSize = bufferSize;
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(Stream stream, IEnumerable<HashType> hashTypes, IProgress<ulong>? progress, IThrottler throttler, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(stream);

		ct.ThrowIfCancellationRequested();

		List<HashType> requested = hashTypes?.Distinct().ToList() ?? new List<HashType>();

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

				foreach(HashAlgorithm algo in algorithms.Values)
				{
					// Hashing does not need output data.
					// Passing null avoids copying input bytes back to an output buffer.
					algo.TransformBlock(buffer, 0, bytesRead, null, 0);

					// If one of the hash implementations does not like null as output buffer, then shift to this safe variant.
					// but if they support nul, is null the best for hashing, because we do not need the output.
					//algo.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}

				progress?.Report(totalBytesRead);

				// If throttling should be re-enabled later, add it here.
				await throttler.WaitAsync().ConfigureAwait(false);
			}

			ct.ThrowIfCancellationRequested();

			Dictionary<HashType, string> results = new(algorithms.Count);

			foreach(KeyValuePair<HashType, HashAlgorithm> kvp in algorithms)
			{
				kvp.Value.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

				byte[]? hashBytes = kvp.Value.Hash;

				if(hashBytes != null)
				{
					results[kvp.Key] = Convert.ToHexString(hashBytes).ToLowerInvariant();
				}
			}

			return results;
		} finally
		{
			if(buffer != null)
			{
				ArrayPool<byte>.Shared.Return(buffer);
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