using System.Collections.Concurrent;
using System.Security.Cryptography;
using BMTP3.Core2.BackupNew.Engine.Strategies;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

public class DestinationInspector : IDestinationInspector
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _hashCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<FileSnapshot> GetSnapshotAsync(string path, CancellationToken ct)
    {
        var exists = File.Exists(path);
        if (!exists)
        {
            return new FileSnapshot { Exists = false, Length = 0, LastWriteTimeUtc = DateTime.MinValue };
        }

        var fi = new FileInfo(path);
        return new FileSnapshot { Exists = true, Length = (ulong)fi.Length, LastWriteTimeUtc = fi.LastWriteTimeUtc };    
    }

    public async Task<string> GetHashAsync(string path, string algorithm, CancellationToken ct)
    {
        if (!_hashCache.TryGetValue(path + "|" + algorithm, out var cached))
        {
            var sem = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1,1));
            await sem.WaitAsync(ct);
            try
            {
                // double-check cache under lock
                if (_hashCache.TryGetValue(path + "|" + algorithm, out cached))
                    return cached;

                // compute
                string hash = await ComputeFileHashAsync(path, algorithm, ct);
                _hashCache[path + "|" + algorithm] = hash;

                return hash;
            }
            finally
            {
                sem.Release();
            }
        }

        return cached!;
    }

    private async Task<string> ComputeFileHashAsync(string filePath, string algorithm, CancellationToken ct)
    {
        if (string.Equals(algorithm, "SHA256", StringComparison.OrdinalIgnoreCase))
        {
            using var sha256 = SHA256.Create();
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var hashBytes = await sha256.ComputeHashAsync(stream, ct);
            return BitConverter.ToString(hashBytes).Replace("-","").ToLowerInvariant();
        }

        throw new NotSupportedException($"Algorithm {algorithm} is not supported by DestinationInspector.");
    }
}
