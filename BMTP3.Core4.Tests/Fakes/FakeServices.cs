using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.DriveDiscovery;
using BMTP3.Core4.Engine.DiskSpace;
using BMTP3.Core4.Engine.Downloader;
using BMTP3.Core4.Engine.Index;
using BMTP3.Core4.Engine.Sidecar;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Engine.TimeStamp;
using BMTP3.Core4.Infrastructure.Throttling;
using BMTP3.Core4.Models;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.Storage;
using BMTP3.Core4.Traversal;
using System.Runtime.CompilerServices;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeBackupScanner : IBackupScanner
{
	private readonly IReadOnlyList<BackupItem> _items;

	public FakeBackupScanner(IReadOnlyList<BackupItem> items)
	{
		_items = items;
	}

	public async IAsyncEnumerable<BackupItem> ScanAsync(
		ISourceTraversal traversal,
		BackupScanRequest request,
		IProgress<BackupScanProgress>? progress,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		foreach(BackupItem item in _items)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return item;
		}
	}
}

internal sealed class FakeSourceTraversalFactory : ISourceTraversalFactory
{
	private readonly ISourceTraversal _traversal;

	public FakeSourceTraversalFactory(ISourceTraversal traversal)
	{
		_traversal = traversal;
	}

	public ISourceTraversal Create(IConnectedSource connectedSource) => _traversal;
}

internal sealed class FakeDriveProvider : IDriveProvider
{
	private readonly IReadOnlyList<IBackupDriveInfo> _drives;

	public FakeDriveProvider(IReadOnlyList<IBackupDriveInfo> drives)
	{
		_drives = drives;
	}

	public IReadOnlyList<IBackupDriveInfo> ListDrives() => _drives;
}

internal sealed class FakeSourceConnector : ISourceConnector
{
	private readonly IConnectedSource _connectedSource;

	public FakeSourceConnector(IConnectedSource connectedSource)
	{
		_connectedSource = connectedSource;
	}

	public IConnectedSource Connect(IBackupDriveInfo backupDriveInfo) => _connectedSource;
}

internal sealed class FakeDownloadService : IDownloadService
{
	public Task DownloadAsync(DownloadRequest request, IProgress<ulong>? progress, CancellationToken cancellationToken)
	{
		progress?.Report(request.Item.Content.Length);
		return Task.CompletedTask;
	}
}

internal sealed class FakeEarliestTimestampResolutionService : IEarliestTimestampResolutionService
{
	public DateTimeOffset? FakeTimestamp { get; set; } = DateTimeOffset.UtcNow;

	public Task<EarliestTimestampResolutionResult> ResolveAndApplyEarliestAsync(
		EarliestTimestampResolutionRequest request,
		bool enableTimestampCorrection,
		CancellationToken cancellationToken)
	{
		return Task.FromResult(new EarliestTimestampResolutionResult
		{
			Timestamp = FakeTimestamp,
		});
	}
}

internal sealed class FakeDiskSpaceValidator : IDiskSpaceValidator
{
	public Task EnsureMinimumFreeSpaceAsync(string destinationPath, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public Task EnsureSufficientBackupCapacityAsync(string destinationPath, long totalBytesRequired, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}

internal sealed class FakeTargetPathResolver : ITargetPathResolver
{
	public string Resolve(TargetPathResolveRequest request)
	{
		return Path.Combine(request.DestinationRoot, request.RelativeDirectoryPath, request.FileName);
	}
}

internal sealed class FakeCollisionResolver : ICollisionResolver
{
	public Task<CollisionResult> ResolveAsync(CollisionResolveRequest request, IThrottler throttler, CancellationToken cancellationToken)
	{
		return Task.FromResult(new CollisionResult(
			CollisionResolutionAction.Move,
			request.IntendedTargetPath
		));
	}
}

internal sealed class FakeBackupIndexWriter : IBackupIndexWriter
{
	public int WriteCount { get; private set; }

	public Task WriteAsync(
		string destinationDirectory,
		string sessionId,
		IReadOnlyList<BackupRecord> records,
		Api.Models.BackupPlan plan,
		Api.Models.BackupResult result,
		CancellationToken cancellationToken)
	{
		WriteCount++;
		return Task.CompletedTask;
	}
}

internal sealed class FakeSidecarService : ISidecarService
{
	public int WriteCount { get; private set; }

	public Task WriteAsync(string targetFilePath, SidecarRequest request, CancellationToken cancellationToken)
	{
		WriteCount++;
		return Task.CompletedTask;
	}
}

internal sealed class FakeSourceTraversal : ISourceTraversal
{
	public async IAsyncEnumerable<SourceTraversalItem> TraverseAsync(
		SourceTraversalRequest request,
		IProgress<SourceTraversalProgress>? progress,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		yield break;
	}
}

internal sealed class FakeConnectedSource : IConnectedSource
{
	public string Name => "FakeConnectedSource";
	public void Dispose() { }
}
