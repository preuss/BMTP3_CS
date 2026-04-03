using BMTP3.Core2.BackupNew.Api.Request.Enums;

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
		Dictionary<HashType, string> empty = new();
		return Task.FromResult(empty);
	}
}