using BMTP3.Core3.Transfer;

namespace BMTP3.Core3.Tests.Fakes;

/// <summary>
/// Fake file transfer for testing that tracks calls but doesn't write files.
/// </summary>
public class FakeFileTransfer : IFileTransfer
{
	public List<(BackupItem Item, string Destination)> CopiedItems { get; } = new();
	public bool ShouldFail { get; set; }
	public string? FailureNamePattern { get; set; }

   public Task<string> CopyAsync(BackupItem item, string destination, IProgress<long>? progress, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		// Check for pattern-based failure
		if (!string.IsNullOrEmpty(FailureNamePattern) && item.Name.Contains(FailureNamePattern))
		{
			throw new InvalidOperationException($"Fake transfer failure for pattern: {FailureNamePattern}");
		}

		if (ShouldFail)
		{
			throw new InvalidOperationException("Fake transfer configured to fail");
		}

		CopiedItems.Add((item, destination));
		progress?.Report(item.SizeInBytes);
      return Task.FromResult(Path.Combine(destination, item.Name));
	}
}
