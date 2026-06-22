using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine;
using BMTP3.Core4.DriveDiscovery;
using BMTP3.Core4.Engine.DiskSpace;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Engine.Sidecar;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Engine.TimeStamp;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Helpers;
using BMTP3.Core4.Infrastructure.Throttling;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Scanner;
using BMTP3.Core4.Storage;
using BMTP3.Core4.Traversal;
using BMTP3.Core4.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core4.Tests.Engine;

public class BackupEngineErrorPathTests
{
	[Fact]
	public async Task DownloadFailure_StopOnErrorTrue_Rethrows()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: true);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);

			FakeDownloadService downloadService = new() { FailOnItemIndex = 1 };

			BackupEngine engine = CreateEngine(
				items: new[] { item1, item2 },
				testDir: testDir,
				drive: drive,
				downloadService: downloadService);

			await Assert.ThrowsAsync<IOException>(() => engine.RunAsync(plan, null, CancellationToken.None));
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task DownloadFailure_StopOnErrorFalse_ReturnsFailedResult()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: false);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);

			FakeDownloadService downloadService = new() { FailOnItemIndex = 1 };
			FakeSidecarService sidecarService = new();

			BackupEngine engine = CreateEngine(
				items: new[] { item1, item2 },
				testDir: testDir,
				drive: drive,
				downloadService: downloadService,
				sidecarService: sidecarService);

			BackupResult result = await engine.RunAsync(plan, null, CancellationToken.None);

			Assert.Equal(BackupResultState.Failed, result.State);
			Assert.Equal(2, result.ItemResults.Count);
			Assert.Equal(BackupResultItemState.Succeeded, result.ItemResults[0].State);
			Assert.Equal(BackupResultItemState.Failed, result.ItemResults[1].State);
			Assert.Equal(1, sidecarService.WriteCount);
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task TargetPathResolverFailure_StopOnErrorTrue_Rethrows()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: true);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);

			BackupEngine engine = CreateEngine(
				items: new[] { item1, item2 },
				testDir: testDir,
				drive: drive,
				targetPathResolver: new ThrowingTargetPathResolver());

			await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunAsync(plan, null, CancellationToken.None));
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task TargetPathResolverFailure_StopOnErrorFalse_Continues()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: false);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);

			BackupEngine engine = CreateEngine(
				items: new[] { item1, item2 },
				testDir: testDir,
				drive: drive,
				targetPathResolver: new ThrowingTargetPathResolver());

			BackupResult result = await engine.RunAsync(plan, null, CancellationToken.None);

			Assert.Equal(BackupResultState.Failed, result.State);
			Assert.Equal(2, result.ItemResults.Count);
			Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Failed, r.State));
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task SidecarWriteFailure_StopOnErrorFalse_Continues()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: false);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);

			BackupEngine engine = CreateEngine(
				items: new[] { item1, item2 },
				testDir: testDir,
				drive: drive,
				sidecarService: new ThrowingSidecarService());

			BackupResult result = await engine.RunAsync(plan, null, CancellationToken.None);

			Assert.Equal(BackupResultState.Failed, result.State);
			Assert.Equal(2, result.ItemResults.Count);
			Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Failed, r.State));
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task HashServiceFailure_StopOnErrorFalse_Continues()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: false);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);

			BackupEngine engine = CreateEngine(
				items: new[] { item1, item2 },
				testDir: testDir,
				drive: drive,
				hashService: new ThrowingHashService());

			BackupResult result = await engine.RunAsync(plan, null, CancellationToken.None);

			Assert.Equal(BackupResultState.Failed, result.State);
			Assert.Equal(2, result.ItemResults.Count);
			Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Failed, r.State));
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task TimestampResolutionFailure_StopOnErrorFalse_Continues()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: false);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);

			BackupEngine engine = CreateEngine(
				items: new[] { item1, item2 },
				testDir: testDir,
				drive: drive,
				timestampService: new NullTimestampService());

			BackupResult result = await engine.RunAsync(plan, null, CancellationToken.None);

			Assert.Equal(BackupResultState.Failed, result.State);
			Assert.Equal(2, result.ItemResults.Count);
			Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Failed, r.State));
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task MixedSuccessAndFailure_StopOnErrorFalse_ReturnsFailedState()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: false);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);
			BackupItem item3 = CreateBackupItem("notes.txt", "Docs", sourceRoot);

			FakeDownloadService downloadService = new() { FailOnItemIndex = 1 };
			FakeSidecarService sidecarService = new();

			BackupEngine engine = CreateEngine(
				items: new[] { item1, item2, item3 },
				testDir: testDir,
				drive: drive,
				downloadService: downloadService,
				sidecarService: sidecarService);

			BackupResult result = await engine.RunAsync(plan, null, CancellationToken.None);

			Assert.Equal(BackupResultState.Failed, result.State);
			Assert.Equal(3, result.ItemResults.Count);
			Assert.Equal(BackupResultItemState.Succeeded, result.ItemResults[0].State);
			Assert.Equal(BackupResultItemState.Failed, result.ItemResults[1].State);
			Assert.Equal(BackupResultItemState.Succeeded, result.ItemResults[2].State);
			Assert.Equal(2, sidecarService.WriteCount);
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task EmptyDriveList_ThrowsInvalidOperation()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: true);

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);

			FakeDriveProvider driveProvider = new(Array.Empty<IBackupDriveInfo>());

			BackupEngine engine = CreateEngine(
				items: new[] { item1 },
				testDir: testDir,
				driveProvider: driveProvider);

			InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => engine.RunAsync(plan, null, CancellationToken.None));
			Assert.Contains("No drive found", ex.Message);
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task NoMatchingDrive_ThrowsInvalidOperation()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: true);

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);

			FakeDriveProvider driveProvider = new(new IBackupDriveInfo[]
			{
				new FakeBackupDriveInfo("file:///Z:/", "Z:"),
			});

			BackupEngine engine = CreateEngine(
				items: new[] { item1 },
				testDir: testDir,
				driveProvider: driveProvider);

			InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
				() => engine.RunAsync(plan, null, CancellationToken.None));
			Assert.Contains("No drive found", ex.Message);
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task TraversalFailure_FailFast()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: true);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupEngine engine = CreateEngine(
				items: Array.Empty<BackupItem>(),
				testDir: testDir,
				drive: drive,
				scanner: new ThrowingBackupScanner(new UnauthorizedAccessException("Access denied to directory.")));

			UnauthorizedAccessException ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
				() => engine.RunAsync(plan, null, CancellationToken.None));
			Assert.Contains("Access denied", ex.Message);
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	[Fact]
	public async Task ScanPhaseCancellation_ReturnsCancelledResult()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_ErrorTest_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir, stopOnError: true);

			FakeBackupDriveInfo drive = new(
				PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

			BackupEngine engine = CreateEngine(
				items: Array.Empty<BackupItem>(),
				testDir: testDir,
				drive: drive,
				scanner: new ThrowingBackupScanner(new OperationCanceledException()));

			BackupResult result = await engine.RunAsync(plan, null, CancellationToken.None);

			Assert.Equal(BackupResultState.Cancelled, result.State);
		}
		finally
		{
			if(Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	// ----------------------------------------------------------------
	// Fakes
	// ----------------------------------------------------------------

	private sealed class ThrowingBackupScanner : IBackupScanner
	{
		private readonly Exception _exception;

		public ThrowingBackupScanner(Exception exception)
		{
			_exception = exception;
		}

		public IAsyncEnumerable<BackupItem> ScanAsync(
			ISourceTraversal traversal,
			BackupScanRequest request,
			IProgress<BackupScanProgress>? progress,
			CancellationToken cancellationToken)
		{
			throw _exception;
		}
	}

	private sealed class ThrowingTargetPathResolver : ITargetPathResolver
	{
		public string Resolve(TargetPathResolveRequest request)
			=> throw new InvalidOperationException("Simulated TargetPathResolver failure.");
	}

	private sealed class ThrowingSidecarService : ISidecarService
	{
		public Task WriteAsync(string targetFilePath, SidecarRequest request, CancellationToken cancellationToken)
			=> throw new IOException("Simulated sidecar write failure.");
	}

	private sealed class ThrowingHashService : IHashService
	{
		public Task<Dictionary<HashType, string>> ComputeHashesAsync(
			IContent content,
			string relativePath,
			IReadOnlyCollection<HashAlgorithmType> algorithms,
			IProgress<ulong>? progress,
			IThrottler throttler,
			CancellationToken ct)
			=> Task.FromException<Dictionary<HashType, string>>(
				new InvalidOperationException("Simulated HashService failure."));
	}

	private sealed class NullTimestampService : IEarliestTimestampResolutionService
	{
		public Task<EarliestTimestampResolutionResult> ResolveAndApplyEarliestAsync(
			EarliestTimestampResolutionRequest request,
			bool enableTimestampCorrection,
			CancellationToken cancellationToken)
			=> Task.FromResult(new EarliestTimestampResolutionResult { Timestamp = null });
	}

	private sealed class FakeBackupDriveInfo : IBackupFileSystemDriveInfo
	{
		public FakeBackupDriveInfo(string rootPath, string driveName)
		{
			RootPath = rootPath;
			DriveName = driveName;
			Id = driveName;
			DisplayName = driveName;
		}

		public string Id { get; }
		public string DriveName { get; }
		public string DisplayName { get; }
		public BackupSourceType SourceType => BackupSourceType.FileSystem;
		public string RootPath { get; }
		public long TotalSize => 1024L * 1024 * 1024;
		public long AvailableFreeSpace => 512L * 1024 * 1024;
		public string VolumeLabel => DriveName;
		public string DriveFormat => "NTFS";
		public DriveType DriveType => DriveType.Fixed;
	}

	// ----------------------------------------------------------------
	// Engine factory
	// ----------------------------------------------------------------

	private static BackupEngine CreateEngine(
		IReadOnlyList<BackupItem> items,
		string testDir,
		FakeBackupDriveInfo? drive = null,
		FakeDownloadService? downloadService = null,
		IBackupScanner? scanner = null,
		ITargetPathResolver? targetPathResolver = null,
		ICollisionResolver? collisionResolver = null,
		ISidecarService? sidecarService = null,
		IHashService? hashService = null,
		IEarliestTimestampResolutionService? timestampService = null,
		IDiskSpaceValidator? diskSpaceValidator = null,
		IDriveProvider? driveProvider = null,
		ISourceConnector? sourceConnector = null,
		ISourceTraversalFactory? traversalFactory = null)
	{
		string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";
		FakeBackupDriveInfo actualDrive = drive ?? new FakeBackupDriveInfo(
			PathHelper.ToInternalCanonicalUri(driveRoot, BackupSourceType.FileSystem), driveRoot.TrimEnd('\\'));

		return new BackupEngine(
			scanner: scanner ?? new FakeBackupScanner(items),
			sourceTraversalFactory: traversalFactory ?? new FakeSourceTraversalFactory(new FakeSourceTraversal()),
			driveProvider: driveProvider ?? new FakeDriveProvider(new[] { actualDrive }),
			sourceConnector: sourceConnector ?? new FakeSourceConnector(new FakeConnectedSource()),
			downloadService: downloadService ?? new FakeDownloadService(),
			hashService: hashService ?? new FakeHashService(),
			earliestTimestampService: timestampService ?? new FakeEarliestTimestampResolutionService
			{
				FakeTimestamp = DateTimeOffset.UtcNow.AddDays(-7),
			},
			sidecarService: sidecarService ?? new FakeSidecarService(),
			diskSpaceValidator: diskSpaceValidator ?? new FakeDiskSpaceValidator(),
			logger: NullLogger<BackupEngine>.Instance,
			targetPathResolver: targetPathResolver ?? new FakeTargetPathResolver(),
			collisionResolver: collisionResolver ?? new FakeCollisionResolver(),
			backupIndexWriter: new FakeBackupIndexWriter()
		);
	}

	// ----------------------------------------------------------------
	// Helpers
	// ----------------------------------------------------------------

	private static BackupPlan CreateTestPlan(string sourcePath, string destination, bool stopOnError)
	{
		return new BackupPlan
		{
			Name = "ErrorPathTest",
			SourceType = BackupSourceType.FileSystem,
			SourcePath = sourcePath,
			Destination = destination,
			Recursive = true,
			DryRun = false,
			StopOnError = stopOnError,
			BackupIndexType = BackupIndexType.None,
			PostWriteVerification = PostWriteVerificationType.None,
			SidecarFormat = SidecarFormat.Ini,
			CollisionStrategy = CollisionStrategy.Rename,
			CollisionComparisonType = CollisionComparisonType.Binary,
			RenameStrategy = RenameStrategy.Increment,
			ComparisonHashAlgorithmTypes = new[] { HashAlgorithmType.SHA2_256 },
			VerificationHashAlgorithmTypes = new[] { HashAlgorithmType.SHA2_256 },
			OutputStructureStrategy = OutputStructureStrategy.PreserveHierarchy,
			ResumeBehavior = SessionResumeStrategy.Continue,
			Delay = 0,
			EnableTimestampCorrection = true,
		};
	}

	private static BackupItem CreateBackupItem(string fileName, string relativeDir, string sourceRoot)
	{
		string relativePath = string.IsNullOrEmpty(relativeDir) ? fileName : $"{relativeDir}\\{fileName}";
		string fullPath = Path.Combine(sourceRoot, relativePath);

		return new BackupItem(new FakeMoveableContent($"Content of {fileName}"))
		{
			Id = Guid.NewGuid().ToString("N"),
			SourcePath = fullPath,
			RelativeFilePath = relativePath,
			FileName = fileName,
			DateCreated = DateTimeOffset.UtcNow.AddDays(-10),
			DateModified = DateTimeOffset.UtcNow.AddDays(-5),
			DateAuthored = DateTimeOffset.UtcNow.AddDays(-7),
			DateAccessed = DateTimeOffset.UtcNow,
		};
	}
}
