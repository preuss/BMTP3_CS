using Blake3;
using BMTP3.Core4.Hashing.Crypto;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Security.Cryptography;


namespace BMTP3.Core4.Hashing;

public class HashCalculator
{
	/// <summary>
	///     Computes one or more hashes over the given file in a single streaming pass.
	/// </summary>
	/// <param name="filePath">The path to the file to hash.</param>
	/// <param name="hashTypes">The list of hash algorithms to compute.</param>
	/// <param name="bufferSize">
	///     The size of the buffer used for reading the file (default: 8 KB for balanced memory and I/O
	///     performance).
	/// </param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A read-only dictionary containing the computed hashes, keyed by hash type.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="hashTypes" /> is null or empty.</exception>
	/// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
	/// <exception cref="IOException">Thrown if there is an error accessing the file.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize" /> is less than or equal to zero.</exception>
	/// <exception cref="NotSupportedException">Thrown if an unsupported hash type is provided.</exception>
	public IReadOnlyDictionary<HashType, string> ComputeHashes(
		string filePath,
		IList<HashType> hashTypes,
		int bufferSize = 8 * 1024,
		CancellationToken cancellationToken = default
	)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException("filePath cannot be null or empty.", nameof(filePath));
		}

		if (hashTypes == null || hashTypes.Count == 0)
		{
			throw new ArgumentException("hashTypes cannot be null or empty.", nameof(hashTypes));
		}

		// We test for Exists inside the OpenRead.
		//if(!File.Exists(filePath)) {
		//	throw new FileNotFoundException("File does not exist.", filePath);
		//}
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be greater than zero.");
		}

		long fileLength = new FileInfo(filePath).Length;
		//Console.WriteLine($"BufferSize input {bufferSize / 1024} * 1024 = {bufferSize}");
		// Allow caller override (only adapt when they passed the default 8 KB)
		if (bufferSize == 8 * 1024)
		{
			bufferSize = fileLength switch
			{
				< 256 * 1024 => 32 * 1024,
				< 4L * 1024 * 1024 => 64 * 1024,
				< 32L * 1024 * 1024 => 256 * 1024,
				< 256L * 1024 * 1024 => 512 * 1024,
				//_ => 512 * 1024 // or 1024 * 1024 if you prefer
				_ => 512 * 1024 // or 1024 * 1024 if you prefer
			};
		}
		//Console.WriteLine($"BufferSize choosen {bufferSize / 1024} * 1024 = {bufferSize}");

		// Deduplicate hash types for efficiency
		List<HashType> requestedHashTypes = hashTypes.Distinct().ToList();

		bool needBlake3_256 = requestedHashTypes.Contains(HashType.BLAKE3_256);
		bool needBlake3_512 = requestedHashTypes.Contains(HashType.BLAKE3_512);
		bool useBlake3 = needBlake3_256 || needBlake3_512;

		ImmutableDictionary<HashType, string>.Builder hashResultsBuilder =
			ImmutableDictionary.CreateBuilder<HashType, string>();
		IDictionary<HashType, HashAlgorithm> hashAlgorithms = new Dictionary<HashType, HashAlgorithm>();
		Hasher? blake3Hasher = useBlake3 ? Hasher.New() : null;

		try
		{
			// Initialize hash algorithms
			foreach (HashType hashType in requestedHashTypes)
			{
				switch (hashType)
				{
					case HashType.SHA3_512_FIPS202:
						//hashAlgorithms[hashType] = SHA3.Net.Sha3.Sha3512();
						hashAlgorithms[hashType] = new SharpHashSHA3_512();
						break;
					case HashType.SHA3_256_FIPS202:
						//hashAlgorithms[hashType] = SHA3.Net.Sha3.Sha3256();
						hashAlgorithms[hashType] = new SharpHashSHA3_256();
						break;
					case HashType.SHA3_512_KECCAK:
						// Legacy Keccak-512 (different padding)
						//hashAlgorithms[hashType] = new BouncyCastleSha3_512_Keccak();
						hashAlgorithms[hashType] = new SharpHashSHA3_512_Keccak();
						break;
					case HashType.SHA3_256_KECCAK:
						// Legacy Keccak-256 (different padding)
						//hashAlgorithms[hashType] = new BouncyCastleSha3_256_Keccak();
						hashAlgorithms[hashType] = new SharpHashSHA3_256_Keccak();
						break;
					case HashType.SHA2_256:
						hashAlgorithms[hashType] = SHA256.Create();
						break;
					case HashType.SHA2_512:
						hashAlgorithms[hashType] = SHA512.Create();
						break;
					case HashType.MD5_128:
						hashAlgorithms[hashType] = MD5.Create();
						//hashAlgorithms[hashType] = new SharpHashMD5();
						break;
					case HashType.BLAKE3_256:
					case HashType.BLAKE3_512:
						// Blake3 is handled separately
						break;
					default:
						throw new NotSupportedException($"Hash type {hashType} is not supported.");
				}
			}

			// Calculate hashes by streaming the file
			try
			{
				// SequentialScan hint, improves performance with big sequential reads.
				//using(FileStream stream = File.OpenRead(filePath)) {
				using (FileStream stream = new(
						   filePath,
						   FileMode.Open,
						   FileAccess.Read,
						   FileShare.Read,
						   bufferSize,
						   FileOptions.SequentialScan)
					  )
				{
					byte[] buffer = new byte[bufferSize];
					int bytesRead;
					while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
					{
						cancellationToken.ThrowIfCancellationRequested();
						foreach (HashAlgorithm hashAlgorithm in hashAlgorithms.Values)
						{
							hashAlgorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
						}

						if (useBlake3)
						{
							blake3Hasher?.Update(buffer.AsSpan(0, bytesRead));
						}
					}
				}
			}
			catch (FileNotFoundException ex)
			{
				throw new FileNotFoundException("File does not exist.", filePath, ex);
			}
			catch (IOException ex)
			{
				throw new IOException($"Error accessing file: {filePath}", ex);
			}

			// Finalize standard hash algorithms
			foreach (HashAlgorithm hashAlgorithm in hashAlgorithms.Values)
			{
				hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
			}

			// Convert standard hash results to hex strings
			foreach (KeyValuePair<HashType, HashAlgorithm> kvp in hashAlgorithms)
			{
				string hashString = kvp.Value.Hash == null ? string.Empty : ToHex(kvp.Value.Hash);
				hashResultsBuilder[kvp.Key] = hashString;
			}

			// Finalize and convert Blake3 results
			if (useBlake3)
			{
				Span<byte> blake3Hash = stackalloc byte[64];
				blake3Hasher?.Finalize(blake3Hash);

				if (needBlake3_256)
				{
					hashResultsBuilder[HashType.BLAKE3_256] = ToHex(blake3Hash.Slice(0, 32));
				}

				if (needBlake3_512)
				{
					hashResultsBuilder[HashType.BLAKE3_512] = ToHex(blake3Hash);
				}
			}

			return new ReadOnlyDictionary<HashType, string>(hashResultsBuilder.ToImmutable());
		}
		finally
		{
			// Cleanup
			foreach (HashAlgorithm hashAlgorithm in hashAlgorithms.Values)
			{
				hashAlgorithm.Dispose();
			}

			blake3Hasher?.Dispose();
		}
	}

	/// <summary>
	///     Converts a byte span to a lowercase hexadecimal string.
	/// </summary>
	private static string ToHex(ReadOnlySpan<byte> bytes)
	{
		return bytes.Length == 0 ? string.Empty : Convert.ToHexString(bytes).ToLowerInvariant();
	}
}