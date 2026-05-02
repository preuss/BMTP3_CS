using BMTP3.Core3.Sidecar;

namespace BMTP3.Core3.Tests.Fakes;

/// <summary>
/// Fake sidecar generator that tracks calls but doesn't write files.
/// </summary>
public class FakeSidecarGenerator : ISidecarGenerator
{
	public List<(BackupItem Item, string Directory)> GeneratedSidecars { get; } = new();
	public bool ShouldFail { get; set; }

	public Task GenerateAsync(BackupItem item, string destinationDirectory, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		if (ShouldFail)
		{
			throw new InvalidOperationException("Fake sidecar generator configured to fail");
		}

		GeneratedSidecars.Add((item, destinationDirectory));
		return Task.CompletedTask;
	}
}
