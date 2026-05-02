using BMTP3.Core3.Scanning;

namespace BMTP3.Core3.Tests.Fakes;

/// <summary>
/// Fake scanner for testing that returns predefined items.
/// </summary>
public class FakeBackupScanner : IBackupScanner
{
	private readonly List<BackupItem> _items;

	public bool ShouldFail { get; set; } = false;

	public FakeBackupScanner(params BackupItem[] items)
	{
		_items = new List<BackupItem>(items ?? Array.Empty<BackupItem>());
	}

	public Task<IEnumerable<BackupItem>> ScanAsync(string source, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();
		if (ShouldFail)
		{
			throw new InvalidOperationException("Fake scanner failure");
		}
		return Task.FromResult(_items.AsEnumerable());
	}
}
