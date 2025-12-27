using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Hashing;

public interface IHashGenerator
{
    /// <summary>
    /// Computes multiple hashes from a single stream in one pass.
    /// </summary>
    Task<Dictionary<HashType, string>> ComputeHashesAsync(
        Stream stream, 
        IEnumerable<HashType> hashTypes, 
        IProgress<ulong> progress,
        CancellationToken ct);
}
