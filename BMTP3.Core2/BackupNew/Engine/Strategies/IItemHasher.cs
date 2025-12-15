using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing; // Corrected using for HashType
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

public interface IItemHasher
{
	Task<Dictionary<HashType, string>> ComputeHashesAsync(IBackupItem item, IEnumerable<HashType> hashTypes, CancellationToken ct);
}