using BMTP3.Core2.Engine.Hashing.Crypto;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace BMTP3.Core2.BackupNew.Engine.Hashing;

public class StreamHashGenerator : IHashGenerator
{
	private readonly ILogger<StreamHashGenerator> _logger;
	private const int BufferSize = 81920; // 80 KB buffer

	public StreamHashGenerator(ILogger<StreamHashGenerator> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashType> hashTypes,
		IProgress<ulong> progress,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(stream);
		List<HashType> requested = hashTypes?.Distinct().ToList() ?? new List<HashType>();

		if(requested.Count == 0) return new Dictionary<HashType, string>();

		Dictionary<HashType, HashAlgorithm> algorithms = new();
		Dictionary<HashType, string> results = new();

		try
		{
			// 1. Initialize Algorithms
			foreach(HashType type in requested)
			{
				algorithms[type] = CreateAlgorithm(type);
			}

			// 2. Read Stream & Transform
			byte[] buffer = new byte[BufferSize];
			int bytesRead;
			ulong totalBytesRead = 0;

			while((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
			{
				totalBytesRead += (ulong)bytesRead;

				// Update Progress
				progress?.Report(totalBytesRead);

				// Feed data to all hashers
				foreach(var algo in algorithms.Values)
				{
					algo.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}
			}

			// 3. Finalize & Convert to Hex
			foreach(var kvp in algorithms)
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
			foreach(var algo in algorithms.Values)
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

			_ => throw new NotSupportedException($"HashType {type} is not supported by StreamHashGenerator yet.")
		};
	}
}
