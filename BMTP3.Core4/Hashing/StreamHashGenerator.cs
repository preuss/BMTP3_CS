using System.Security.Cryptography;
using HashAlgorithmEnum = BMTP3.Core4.Api.Models.Enums.HashAlgorithm;

namespace BMTP3.Core4.Hashing;

public class StreamHashGenerator : IHashGenerator
{
	private const int BufferSize = 81920;

	public async Task<Dictionary<HashAlgorithmEnum, string>> ComputeHashesAsync(
		Stream stream,
		IEnumerable<HashAlgorithmEnum> hashTypes,
		IProgress<ulong>? progress,
		CancellationToken ct
	)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ct.ThrowIfCancellationRequested();

		List<HashAlgorithmEnum> requested = hashTypes?.Distinct().ToList() ?? new List<HashAlgorithmEnum>();

		if(requested.Count == 0)
		{
			return new Dictionary<HashAlgorithmEnum, string>();
		}

		Dictionary<HashAlgorithmEnum, HashAlgorithm> algorithms = new();
		Dictionary<HashAlgorithmEnum, string> results = new();

		try
		{
			foreach(HashAlgorithmEnum type in requested)
			{
				algorithms[type] = CreateAlgorithm(type);
			}

			byte[] buffer = new byte[BufferSize];
			int bytesRead;
			ulong totalBytesRead = 0;

			while((bytesRead = await stream.ReadAsync(buffer, 0, BufferSize, ct).ConfigureAwait(false)) > 0)
			{
				totalBytesRead += (ulong)bytesRead;
				progress?.Report(totalBytesRead);

				foreach(HashAlgorithm algo in algorithms.Values)
				{
					algo.TransformBlock(buffer, 0, bytesRead, buffer, 0);
				}
			}

			ct.ThrowIfCancellationRequested();

			foreach(KeyValuePair<HashAlgorithmEnum, HashAlgorithm> kvp in algorithms)
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

	private static HashAlgorithm CreateAlgorithm(HashAlgorithmEnum type)
	{
		return type switch
		{
			HashAlgorithmEnum.MD5_128 => MD5.Create(),
			HashAlgorithmEnum.SHA2_256 => SHA256.Create(),
			HashAlgorithmEnum.SHA2_512 => SHA512.Create(),
			HashAlgorithmEnum.SHA3_256_FIPS202 => new SharpHashSHA3_256(),
			HashAlgorithmEnum.SHA3_512_FIPS202 => new SharpHashSHA3_512(),
			HashAlgorithmEnum.SHA3_256_KECCAK => new SharpHashSHA3_256_Keccak(),
			HashAlgorithmEnum.SHA3_512_KECCAK => new SharpHashSHA3_512_Keccak(),
			HashAlgorithmEnum.BLAKE3_256 => new Blake3HashAlgorithm(32),
			HashAlgorithmEnum.BLAKE3_512 => new Blake3HashAlgorithm(64),
			_ => throw new NotSupportedException($"HashAlgorithmEnum {type} is not supported by StreamHashGenerator yet.")
		};
	}
}
