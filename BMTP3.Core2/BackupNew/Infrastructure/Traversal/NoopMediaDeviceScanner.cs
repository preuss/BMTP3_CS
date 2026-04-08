using System.Runtime.CompilerServices;
using BMTP3.Core2.BackupNew.Engine.Traversal;
using MediaDevices;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Infrastructure.Traversal;

/// <summary>
///     A no-op implementation of <see cref="ITraversalScanner{MediaFileInfo}" /> that yields no entries.
///     Registered by default so that DI resolution succeeds when no real MTP device is available.
///     BackupScanner will skip MTP scanning when no device matching the plan's SourceId is found.
/// </summary>
internal sealed class NoopMediaDeviceScanner : ITraversalScanner<MediaFileInfo>
{
	private readonly ILogger<NoopMediaDeviceScanner>? _logger;

	public NoopMediaDeviceScanner(ILogger<NoopMediaDeviceScanner>? logger = null)
	{
		_logger = logger;
	}

	public IEnumerable<MediaFileInfo> Scan(string rootPath, bool recursive = true,
		IProgress<TraversalProgress>? progress = null, CancellationToken cancellationToken = default)
	{
		_logger?.LogDebug(
			"NoopMediaDeviceScanner.Scan called for '{RootPath}' — no MTP device registered, yielding nothing.",
			rootPath);
		return Enumerable.Empty<MediaFileInfo>();
	}

	public async IAsyncEnumerable<MediaFileInfo> ScanAsync(string rootPath, bool recursive = true,
		IProgress<TraversalProgress>? progress = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		_logger?.LogDebug(
			"NoopMediaDeviceScanner.ScanAsync called for '{RootPath}' — no MTP device registered, yielding nothing.",
			rootPath);
		await Task.CompletedTask;
		yield break;
	}
}