using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;

namespace BMTP3.Core3.Hashing;

/// <summary>
/// Stream-based hash generator that computes multiple hash types in a single pass.
/// Efficiently handles all 9 hash algorithms simultaneously.
/// Uses .NET built-in for SHA2 and MD5, BouncyCastle for SHA3 and BLAKE3.
/// </summary>
public class StreamHashGenerator : IHashGenerator
{
	private const int BufferSize = 81920;  // 80 KB
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
		ct.ThrowIfCancellationRequested();

		var requested = hashTypes?.Distinct().ToList() ?? new List<HashType>();
		if (requested.Count == 0)
		{
			return new Dictionary<HashType, string>();
		}

		var results = new Dictionary<HashType, string>();
		var digesters = new Dictionary<HashType, IDigest>();
		var hashAlgorithms = new Dictionary<HashType, HashAlgorithm>();

		try
		{
			// Initialize all digesters and algorithms
			foreach (var hashType in requested)
			{
				var digest = CreateDigest(hashType);
				if (digest != null)
				{
					digesters[hashType] = digest;
				}
				else
				{
					var algo = CreateHashAlgorithm(hashType);
					if (algo != null)
					{
						hashAlgorithms[hashType] = algo;
					}
					else
					{
						throw new NotImplementedException($"Hash algorithm not implemented: {hashType}");
					}
				}
			}

			// Single pass: read stream and feed to all algorithms
			var buffer = new byte[BufferSize];
			int bytesRead;
			ulong totalBytesRead = 0;

			while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
			{
				totalBytesRead += (ulong)bytesRead;
				progress?.Report(totalBytesRead);

				// Feed to BouncyCastle digesters
				foreach (var digest in digesters.Values)
				{
					digest.BlockUpdate(buffer, 0, bytesRead);
				}

				// Feed to .NET hash algorithms
				foreach (var algo in hashAlgorithms.Values)
				{
					algo.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}
			}

			// Finalize BouncyCastle digesters
			ct.ThrowIfCancellationRequested();
			foreach (var kvp in digesters)
			{
				var output = new byte[kvp.Value.GetDigestSize()];
				kvp.Value.DoFinal(output, 0);
				results[kvp.Key] = BitConverter.ToString(output).Replace("-", "").ToLower();
			}

			// Finalize .NET hash algorithms
			foreach (var kvp in hashAlgorithms)
			{
				kvp.Value.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
				results[kvp.Key] = Convert.ToHexString(kvp.Value.Hash!).ToLower();
			}

			_logger.LogDebug("Hashes computed: {HashCount} types, {TotalBytes} bytes", requested.Count, totalBytesRead);
			return results;
		}
		catch (OperationCanceledException)
		{
			_logger.LogWarning("Hash computation cancelled");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Hash computation failed");
			throw;
		}
		finally
		{
			foreach (var algo in hashAlgorithms.Values)
			{
				algo?.Dispose();
			}
		}
	}

	/// <summary>
	/// Create a BouncyCastle digest for algorithms requiring it (SHA3, BLAKE3).
	/// Returns null if this algorithm is handled by .NET built-ins.
	/// </summary>
	private static IDigest? CreateDigest(HashType hashType)
	{
		return hashType switch
		{
			HashType.SHA3_256_FIPS202 => new Sha3Digest(256),
			HashType.SHA3_512_FIPS202 => new Sha3Digest(512),
			HashType.SHA3_256_KECCAK => new KeccakDigest(256),
			HashType.SHA3_512_KECCAK => new KeccakDigest(512),
			HashType.BLAKE3_256 => new Blake3Digest(256),
			HashType.BLAKE3_512 => new Blake3Digest(512),
			_ => null
		};
	}

	/// <summary>
	/// Create a .NET HashAlgorithm for SHA2 and MD5.
	/// Returns null if this algorithm should use BouncyCastle instead.
	/// </summary>
	private static HashAlgorithm? CreateHashAlgorithm(HashType hashType)
	{
		return hashType switch
		{
			HashType.SHA2_256 => SHA256.Create(),
			HashType.SHA2_512 => SHA512.Create(),
			HashType.MD5_128 => MD5.Create(),
			_ => null
		};
	}
}

/// <summary>
/// BouncyCastle BLAKE3 Digest implementation wrapper.
/// BLAKE3 is not in .NET standard library, so we use BouncyCastle.
/// </summary>
internal class Blake3Digest : IDigest
{
	private readonly Org.BouncyCastle.Crypto.Digests.Blake3Digest _digest;
	private readonly int _outputLength;

	public Blake3Digest(int outputBits)
	{
		_outputLength = outputBits / 8;
		_digest = new Org.BouncyCastle.Crypto.Digests.Blake3Digest();
	}

	public string AlgorithmName => $"BLAKE3-{_outputLength * 8}";
	public int GetDigestSize() => _outputLength;
	public int GetByteLength() => 64;

	public void BlockUpdate(byte[] input, int inOff, int length) => _digest.BlockUpdate(input, inOff, length);
	public void BlockUpdate(ReadOnlySpan<byte> input) => _digest.BlockUpdate(input);
	public void Update(byte input) => _digest.Update(input);
	public int DoFinal(byte[] output, int outOff) => _digest.DoFinal(output, outOff);
	public int DoFinal(Span<byte> output) => _digest.DoFinal(output);
	public void Reset() => _digest.Reset();
}

/// <summary>
/// Item-level hash generator: wraps StreamHashGenerator for BackupItem files.
/// </summary>
public class ItemHasher : IItemHasher
{
	private readonly IHashGenerator _hashGenerator;
	private readonly ILogger<ItemHasher> _logger;

	public ItemHasher(ILogger<ItemHasher> logger, IHashGenerator hashGenerator)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_hashGenerator = hashGenerator ?? throw new ArgumentNullException(nameof(hashGenerator));
	}

	public async Task<Dictionary<HashType, string>> ComputeHashesAsync(
		BackupItem item,
		List<HashType> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		ct.ThrowIfCancellationRequested();

		var requested = (hashTypes ?? new List<HashType>()).Distinct().ToList();
		if (requested.Count == 0)
		{
			requested.Add(HashType.SHA2_256);  // Default if none specified
		}

		_logger.LogDebug("Computing hashes for {ItemPath} with {HashCount} types", item.SourcePath, requested.Count);

		try
		{
			using var stream = File.OpenRead(item.SourcePath);
			return await _hashGenerator.ComputeHashesAsync(stream, requested, progress, ct).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to compute hashes for {ItemPath}", item.SourcePath);
			throw;
		}
	}
}

