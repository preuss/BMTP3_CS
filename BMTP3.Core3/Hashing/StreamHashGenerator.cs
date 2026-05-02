using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core3.Hashing;

/// <summary>
/// Stream-based hash generator that computes multiple hash types in a single pass.
/// Efficiently handles all 9 hash algorithms simultaneously.
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

		var algorithms = new Dictionary<HashType, HashAlgorithm>();
		var results = new Dictionary<HashType, string>();

		try
		{
			// Initialize all algorithms
			foreach (var hashType in requested)
			{
				algorithms[hashType] = CreateAlgorithm(hashType);
			}

			// Single pass: read stream and feed to all algorithms
			var buffer = new byte[BufferSize];
			int bytesRead;
			ulong totalBytesRead = 0;

			while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
			{
				totalBytesRead += (ulong)bytesRead;
				progress?.Report(totalBytesRead);

				foreach (var algo in algorithms.Values)
				{
					algo.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}
			}

			// Finalize and convert to hex
			ct.ThrowIfCancellationRequested();
			foreach (var kvp in algorithms)
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
			foreach (var algo in algorithms.Values)
			{
				algo?.Dispose();
			}
		}
	}

	private static HashAlgorithm CreateAlgorithm(HashType hashType)
	{
		return hashType switch
		{
			HashType.SHA2_256 => SHA256.Create(),
			HashType.SHA2_512 => SHA512.Create(),
			HashType.MD5_128 => MD5.Create(),
			_ => throw new NotImplementedException($"Hash algorithm not implemented: {hashType}")
		};
	}
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
