using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Hashing;
// Minimal hash generator that returns an empty result set (placeholder).
public class NoopHashGenerator : IHashGenerator
{
	public Task<Dictionary<HashType, string>> ComputeHashesAsync(
        Stream dataStream, 
        IEnumerable<HashType> hashTypes, 
        IProgress<ulong> progress,
        CancellationToken ct)
	{
		var empty = new Dictionary<HashType, string>();
		return Task.FromResult(empty);
	}
}