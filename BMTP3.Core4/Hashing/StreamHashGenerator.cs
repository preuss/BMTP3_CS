using BMTP3.Core4.Hashing.Crypto;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace BMTP3.Core4.Hashing;

public class StreamHashGenerator : IHashGenerator
{
	private const int BufferSize = 81920; // 80 KB buffer
	private readonly ILogger<StreamHashGenerator> _logger;

	public StreamHashGenerator(ILogger<StreamHashGenerator> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(stream);
		// Respect cancellation as early as possible
		ct.ThrowIfCancellationRequested();

		List<HashType> requested = hashTypes?.Distinct().ToList() ?? new List<HashType>();

		if(requested.Count == 0)
		{
			return new Dictionary<HashType, string>();
		}

		Dictionary<HashType, HashAlgorithm> algorithms = new(requested.Count);

		try
		{
			// Initialize Algorithms
			foreach(HashType type in requested)
			{
				algorithms[type] = CreateAlgorithm(type);
			}

			// Read Stream & Transform
			byte[] buffer = new byte[BufferSize];
			int bytesRead;
			ulong totalBytesRead = 0;

			while((bytesRead = await stream.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false)) > 0)
			{
				totalBytesRead += (ulong)bytesRead;

				// Feed data to all hashers
				foreach(HashAlgorithm algo in algorithms.Values)
				{
					algo.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}

				// Update progress after each read
				progress?.Report(totalBytesRead);
			}

			// Finalize & Convert to Hex
			// Check cancellation before finalizing
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

			// Custom BouncyCastle/SharpHash implementations
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