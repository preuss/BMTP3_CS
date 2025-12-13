using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Hashing;

// Computes required hashes from a stream (streaming, cancellation aware).
public interface IHashGenerator
{
    Task<IReadOnlyDictionary<HashType, string>> ComputeHashesAsync(Stream dataStream, IEnumerable<HashType> hashTypes, CancellationToken ct);
}