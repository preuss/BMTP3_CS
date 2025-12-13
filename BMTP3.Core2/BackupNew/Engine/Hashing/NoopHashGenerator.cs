using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Hashing;
// Minimal hash generator that returns an empty result set (placeholder).
public class NoopHashGenerator : IHashGenerator
{
	public Task<IReadOnlyDictionary<HashType, string>> ComputeHashesAsync(Stream dataStream, IEnumerable<HashType> hashTypes, CancellationToken ct)
	{
		var empty = new ReadOnlyDictionary<HashType, string>(new Dictionary<HashType, string>());
		return Task.FromResult<IReadOnlyDictionary<HashType, string>>(empty);
	}
}
