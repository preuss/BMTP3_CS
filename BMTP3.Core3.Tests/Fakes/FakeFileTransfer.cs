using BMTP3.Core3.Transfer;

namespace BMTP3.Core3.Tests.Fakes;

/// <summary>
/// Fake file transfer for testing that tracks calls but doesn't write files.
/// </summary>
public class FakeFileTransfer : IFileTransfer
{
	public List<(BackupItem Item, string Destination)> CopiedItems { get; } = new();
	public bool ShouldFail { get; set; }

	public Task CopyAsync(BackupItem item, string destination, IProgress<long>? progress, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		if (ShouldFail)
		{
			throw new InvalidOperationException("Fake transfer configured to fail");
		}

		CopiedItems.Add((item, destination));
		progress?.Report(item.SizeInBytes);
		return Task.CompletedTask;
	}
}
