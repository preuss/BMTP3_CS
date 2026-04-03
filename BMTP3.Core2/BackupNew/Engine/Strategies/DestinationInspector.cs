using System.Collections.Concurrent;
using System.Security.Cryptography;
using BMTP3.Core2.Engine.Hashing.Crypto;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

public class DestinationInspector : IDestinationInspector
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CacheEntry> _hashCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _ttl;

    public DestinationInspector(TimeSpan? cacheTtl = null)
    {
        _ttl = cacheTtl ?? TimeSpan.FromMinutes(5);
    }

    private sealed class CacheEntry
    {
        public string Hash { get; init; } = string.Empty;
        public DateTimeOffset Created { get; init; }
        public DateTime FileLastWriteUtc { get; init; }
    }

	public async Task<FileSnapshot> GetSnapshotAsync(string path, CancellationToken ct)
	{
		var exists = File.Exists(path);
		if(!exists)
		{
			return new FileSnapshot { Exists = false, Length = 0, LastWriteTimeUtc = DateTime.MinValue };
		}

		var fi = new FileInfo(path);
		return new FileSnapshot { Exists = true, Length = (ulong)fi.Length, LastWriteTimeUtc = fi.LastWriteTimeUtc };
	}

    public async Task<string> GetHashAsync(string path, string algorithm, CancellationToken ct)
    {
        string key = path + "|" + algorithm;
        if(!_hashCache.TryGetValue(key, out var cached))
        {
            var sem = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(ct);
            try
            {
                // double-check cache under lock
                if(_hashCache.TryGetValue(key, out var existing) && (DateTimeOffset.UtcNow - existing.Created) <= _ttl)
                    return existing.Hash;

                // compute
                var fi = new FileInfo(path);
                string hash = await ComputeFileHashAsync(path, algorithm, ct);
                _hashCache[key] = new CacheEntry { Hash = hash, Created = DateTimeOffset.UtcNow, FileLastWriteUtc = fi.LastWriteTimeUtc };

                return hash;
            } finally
            {
                sem.Release();
            }
        }

        // If cache entry exists, check TTL and file last write time to detect changes
        var fiCheck = new FileInfo(path);
        if((DateTimeOffset.UtcNow - cached.Created) <= _ttl && fiCheck.LastWriteTimeUtc == cached.FileLastWriteUtc)
            return cached.Hash;

        // expired or file changed -> recompute under lock
        var sem2 = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await sem2.WaitAsync(ct);
        try
        {
            if(_hashCache.TryGetValue(key, out var after))
            {
                var fiAfter = new FileInfo(path);
                if((DateTimeOffset.UtcNow - after.Created) <= _ttl && fiAfter.LastWriteTimeUtc == after.FileLastWriteUtc)
                    return after.Hash;
            }

            var fi = new FileInfo(path);
            string hash = await ComputeFileHashAsync(path, algorithm, ct);
            _hashCache[key] = new CacheEntry { Hash = hash, Created = DateTimeOffset.UtcNow, FileLastWriteUtc = fi.LastWriteTimeUtc };
            return hash;
        }
        finally
        {
            sem2.Release();
        }
    }

    protected virtual async Task<string> ComputeFileHashAsync(string filePath, string algorithm, CancellationToken ct)
    {
        using HashAlgorithm hashAlgorithm = CreateAlgorithm(algorithm);
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hashBytes = await hashAlgorithm.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static HashAlgorithm CreateAlgorithm(string algorithm)
    {
        if(string.IsNullOrWhiteSpace(algorithm))
            throw new ArgumentException("Algorithm must not be empty.", nameof(algorithm));

        string name = algorithm.Trim();

        return name.ToUpperInvariant() switch
        {
            "MD5" or "MD5_128" => MD5.Create(),
            "SHA256" or "SHA2_256" => SHA256.Create(),
            "SHA512" or "SHA2_512" => SHA512.Create(),
            "SHA3_256_FIPS202" => new SharpHashSHA3_256(),
            "SHA3_512_FIPS202" => new SharpHashSHA3_512(),
            "SHA3_256_KECCAK" => new SharpHashSHA3_256_Keccak(),
            "SHA3_512_KECCAK" => new SharpHashSHA3_512_Keccak(),
            "BLAKE3_256" => new Blake3HashAlgorithm(32),
            "BLAKE3_512" => new Blake3HashAlgorithm(64),
            _ => throw new NotSupportedException($"Algorithm {algorithm} is not supported by DestinationInspector.")
        };
    }
}
