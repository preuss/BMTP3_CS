using BMTP3.Core3.Metadata;

namespace BMTP3.Core3.Tests.Fakes;

/// <summary>
/// Fake metadata reader that returns predefined metadata for testing.
/// </summary>
public class FakeMetadataReader : IMetadataReader
{
	private readonly Dictionary<string, Dictionary<string, object>> _metadata = new();
	public bool ShouldFail { get; set; }

	public void SetMetadata(string path, Dictionary<string, object> metadata)
	{
		_metadata[path] = metadata;
	}

	public Task<Dictionary<string, object>> ReadAsync(string path, CancellationToken ct = default)
	{
		ct.ThrowIfCancellationRequested();

		if (ShouldFail)
		{
			throw new InvalidOperationException("Fake metadata reader configured to fail");
		}

		if (_metadata.TryGetValue(path, out var metadata))
		{
			return Task.FromResult(metadata);
		}

		// Return empty metadata if not configured
		return Task.FromResult(new Dictionary<string, object>());
	}
}
