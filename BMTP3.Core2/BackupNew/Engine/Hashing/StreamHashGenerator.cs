using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.Engine.Hashing.Crypto;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Hashing;

public class StreamHashGenerator : IHashGenerator
{
	private const int BufferSize = 81920; // 80 KB buffer
	private readonly ILogger<StreamHashGenerator> _logger;

	// Optional: max read timeout for stream read loops (ms). 0 == no timeout.
	private readonly int _readTimeoutMs;

	public StreamHashGenerator(ILogger<StreamHashGenerator> logger, int readTimeoutMs = 0)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_readTimeoutMs = readTimeoutMs;
	}

	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong> progress,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(stream);
		// Respect cancellation as early as possible
		ct.ThrowIfCancellationRequested();
		List<HashType> requested = hashTypes?.Distinct().ToList() ?? new List<HashType>();

		if (requested.Count == 0)
		{
			return new Dictionary<HashType, string>();
		}

		Dictionary<HashType, HashAlgorithm> algorithms = new();
		Dictionary<HashType, string> results = new();

		try
		{
			// 1. Initialize Algorithms
			foreach (HashType type in requested)
			{
				algorithms[type] = CreateAlgorithm(type);
			}

			// 2. Read Stream & Transform
			byte[] buffer = new byte[BufferSize];
			int bytesRead;
			ulong totalBytesRead = 0;

			while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
			{
				totalBytesRead += (ulong)bytesRead;

				// Update Progress
				progress?.Report(totalBytesRead);

				// Optional read timeout: if a read stalls due to IO, check cancellation/read-timeout.
				if (_readTimeoutMs > 0)
				{
					// no-op here because ReadAsync already respects ct; higher-level callers can pass a linked token with timeout.
				}

				// Feed data to all hashers
				foreach (HashAlgorithm algo in algorithms.Values)
				{
					algo.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}
			}

			// 3. Finalize & Convert to Hex
			// Check cancellation before finalizing
			ct.ThrowIfCancellationRequested();
			foreach (KeyValuePair<HashType, HashAlgorithm> kvp in algorithms)
			{
				kvp.Value.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

				byte[]? hashBytes = kvp.Value.Hash;
				if (hashBytes != null)
				{
					results[kvp.Key] = Convert.ToHexString(hashBytes).ToLowerInvariant();
				}
			}

			return results;
		}
		finally
		{
			foreach (HashAlgorithm algo in algorithms.Values)
			{
				algo.Dispose();
			}
		}
	}

	private HashAlgorithm CreateAlgorithm(HashType type)
	{
		return type switch
		{
			HashType.MD5_128 => MD5.Create(),
			HashType.SHA2_256 => SHA256.Create(),
			HashType.SHA2_512 => SHA512.Create(),

			// Custom BouncyCastle/SharpHash implementations
			HashType.SHA3_256_FIPS202 => new SharpHashSHA3_256(),
			HashType.SHA3_512_FIPS202 => new SharpHashSHA3_512(),
			HashType.SHA3_256_KECCAK => new SharpHashSHA3_256_Keccak(),
			HashType.SHA3_512_KECCAK => new SharpHashSHA3_512_Keccak(),

			HashType.BLAKE3_256 => new Blake3HashAlgorithm(32),
			HashType.BLAKE3_512 => new Blake3HashAlgorithm(64),

			_ => throw new NotSupportedException($"HashType {type} is not supported by StreamHashGenerator yet.")
		};
	}
}