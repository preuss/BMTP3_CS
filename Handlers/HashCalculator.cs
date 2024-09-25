using Blake3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Handlers {
	public class HashCalculator {
		public IDictionary<HashType, string> ComputeHashes(string filePath, IList<HashType> hashTypes, int bufferSize = 8 * 1024) {
			var hashResults = new Dictionary<HashType, string>();

			using(FileStream stream = File.OpenRead(filePath)) {
				byte[] buffer = new byte[bufferSize];
				int bytesRead;

				IDictionary<HashType, HashAlgorithm> hashAlgorithms = new Dictionary<HashType, HashAlgorithm>();
				Blake3.Hasher? blake3Hasher = null;

				foreach(var hashType in hashTypes) {
					switch(hashType) {
						case HashType.SHA3_512_KECCAK:
							hashAlgorithms[hashType] = SHA3.Net.Sha3.Sha3512();
							break;
						case HashType.SHA3_512_FIPS202:
							hashAlgorithms[hashType] = new OnixLabs.Security.Cryptography.Sha3Hash512();
							break;
						case HashType.SHA2_256:
							hashAlgorithms[hashType] = SHA256.Create();
							break;
						case HashType.SHA2_512:
							hashAlgorithms[hashType] = SHA512.Create();
							break;
						case HashType.MD5_128:
							hashAlgorithms[hashType] = MD5.Create();
							break;
						case HashType.BLAKE3_256:
						case HashType.BLAKE3_512:
							// Blake3 is handled separately
							blake3Hasher = Blake3.Hasher.New();
							break;
						default:
							throw new NotSupportedException($"Hash type {hashType} is not supported.");
					}
				}

				// Calculate
				while((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0) {
					foreach(var hashAlgorithm in hashAlgorithms.Values) {
						hashAlgorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
					}
					if(hashTypes.Contains(HashType.BLAKE3_256)) {
						blake3Hasher?.Update(buffer.AsSpan(0, bytesRead));
					}
				}

				// Finalize
				foreach(var hashAlgorithm in hashAlgorithms.Values) {
					hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
				}

				// Hash ToString
				foreach(var kvp in hashAlgorithms) {
					string hashString = BitConverter.ToString(kvp.Value.Hash ?? Array.Empty<byte>()).Replace("-", "").ToLower();
					hashResults[kvp.Key] = hashString;
					kvp.Value.Dispose();
				}
				// Finalize Special Blake and Hash ToString
				if(hashTypes.Contains(HashType.BLAKE3_256) ||
					hashTypes.Contains(HashType.BLAKE3_512)) {
					Span<byte> blake3Hash = stackalloc byte[64];
					blake3Hasher?.Finalize(blake3Hash);

					if(hashTypes.Contains(HashType.BLAKE3_256)) {
						hashResults[HashType.BLAKE3_256] = BitConverter.ToString(blake3Hash.Slice(0, 32).ToArray()).Replace("-", "").ToLower();
					}
					if(hashTypes.Contains(HashType.BLAKE3_512)) {
						hashResults[HashType.BLAKE3_512] = BitConverter.ToString(blake3Hash.ToArray()).Replace("-", "").ToLower();
					}
					blake3Hasher?.Dispose();
				}
			}

			return hashResults;
		}
		public enum HashType {
			SHA3_512_KECCAK,
			SHA3_512_FIPS202,
			SHA2_256,
			SHA2_512,
			MD5_128,
			BLAKE3_256,
			BLAKE3_512
		}
	}
}
