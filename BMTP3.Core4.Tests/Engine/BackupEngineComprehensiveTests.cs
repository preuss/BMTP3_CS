using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine;
using BMTP3.Core4.Engine.Strategies;
using BMTP3.Core4.Models;
using BMTP3.Core4.Storage;
using BMTP3.Core4.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core4.Tests.Engine;

public class BackupEngineComprehensiveTests
{
	private static readonly DateTimeOffset FakeTimestamp = DateTimeOffset.UtcNow.AddDays(-7);

	[Fact]
	public async Task RunAsync_HappyPath_AllItemsSucceed()
	{
		using TestBackupContext ctx = new(2);
		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
		Assert.Equal(2, result.ItemResults.Count);
		Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Succeeded, r.State));
		Assert.Equal(2, ctx.SidecarService.WriteCount);
		Assert.Equal(0, ctx.BackupIndexWriter.WriteCount);
	}

	[Fact]
	public async Task RunAsync_BackupIndexJson_WritesCatalog()
	{
		using TestBackupContext ctx = new(2, withIndex: true);
		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
		Assert.Equal(1, ctx.BackupIndexWriter.WriteCount);
	}

	[Fact]
	public async Task RunAsync_DryRun_NoDownloadOrSidecar()
	{
		using TestBackupContext ctx = new(3, withDryRun: true);
		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
		Assert.True(result.IsDryRun);
		Assert.Equal(3, result.ItemResults.Count);
		Assert.Equal(0, ctx.SidecarService.WriteCount);
		Assert.Equal(0, ctx.BackupIndexWriter.WriteCount);
	}

	[Fact]
	public async Task RunAsync_StopOnErrorFalse_ContinuesAfterFailure()
	{
		using TestBackupContext ctx = new(3, stopOnError: false);
		ctx.DownloadService.FailOnItemIndex = 1;

		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Failed, result.State);
		Assert.Equal(3, result.ItemResults.Count);
		Assert.Equal(BackupResultItemState.Succeeded, result.ItemResults[0].State);
		Assert.Equal(BackupResultItemState.Failed, result.ItemResults[1].State);
		Assert.Equal(BackupResultItemState.Succeeded, result.ItemResults[2].State);
		Assert.Equal(2, ctx.SidecarService.WriteCount);
	}

	[Fact]
	public async Task RunAsync_StopOnErrorTrue_ThrowsOnFailure()
	{
		using TestBackupContext ctx = new(3, stopOnError: true);
		ctx.DownloadService.FailOnItemIndex = 1;

		await Assert.ThrowsAsync<IOException>(() =>
			ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task RunAsync_CollisionSkip_ItemSkipped()
	{
		using TestBackupContext ctx = new(2, collisionStrategy: CollisionStrategy.Skip);
		ctx.CollisionResolver.DefaultAction = CollisionResolutionAction.Skip;
		CreateDestFiles(ctx.CleanupDir, 2);

		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
		Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Skipped, r.State));
		Assert.Equal(0, ctx.SidecarService.WriteCount);
	}

	[Fact]
	public async Task RunAsync_CollisionOverwrite_ItemOverwritten()
	{
		using TestBackupContext ctx = new(2, collisionStrategy: CollisionStrategy.Overwrite);
		ctx.CollisionResolver.DefaultAction = CollisionResolutionAction.Overwrite;
		CreateDestFiles(ctx.CleanupDir, 2);

		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
		Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Succeeded, r.State));
	}

	private static void CreateDestFiles(string destDir, int count)
	{
		for(int i = 0; i < count; i++)
		{
			string relPath = i % 2 == 0 ? $"file{i:D3}.jpg" : Path.Combine("SubDir", $"file{i:D3}.jpg");
			string fullPath = Path.Combine(destDir, relPath);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
			File.WriteAllText(fullPath, "existing content");
		}
	}

	[Fact]
	public async Task RunAsync_PostWriteVerification_Succeeds()
	{
		using TestBackupContext ctx = new(2, withVerification: true);
		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
		Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Succeeded, r.State));
	}

	[Fact]
	public async Task RunAsync_OutputStrategyFlat_UsesFlatPaths()
	{
		using TestBackupContext ctx = new(2, isFlat: true);
		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
		Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Succeeded, r.State));
	}

	[Fact]
	public async Task RunAsync_SidecarFormatJson_WritesJsonSidecars()
	{
		using TestBackupContext ctx = new(1, useJsonSidecar: true);
		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
		Assert.Equal(1, ctx.SidecarService.WriteCount);
	}

	[Fact]
	public async Task RunAsync_ProgressReports_ContainProgressPhases()
	{
		using TestBackupContext ctx = new(2);
		List<BackupProgress> reports = new();
		SynchronousProgress<BackupProgress> capturingProgress = new(reports);

		await ctx.Engine.RunAsync(ctx.Plan, capturingProgress, TestContext.Current.CancellationToken);

		Assert.Contains(reports, r => r.CurrentPhase == BackupProgressPhase.Starting);
		Assert.Contains(reports, r => r.CurrentPhase == BackupProgressPhase.Transferring);
		Assert.Contains(reports, r => r.CurrentPhase == BackupProgressPhase.Completed);
	}

	[Fact]
	public async Task RunAsync_Delay_DoesNotThrow()
	{
		using TestBackupContext ctx = new(2, withDelay: true);
		BackupResult result = await ctx.Engine.RunAsync(ctx.Plan, ctx.Progress, TestContext.Current.CancellationToken);

		Assert.Equal(BackupResultState.Completed, result.State);
	}

	private sealed class TestBackupContext : IDisposable
	{
		public BackupPlan Plan { get; }
		public BackupEngine Engine { get; }
		public FakeDownloadService DownloadService { get; }
		public FakeSidecarService SidecarService { get; }
		public FakeBackupIndexWriter BackupIndexWriter { get; }
		public FakeCollisionResolver CollisionResolver { get; }
		public IProgress<BackupProgress> Progress { get; }
		public string CleanupDir { get; }

		public TestBackupContext(
			int itemCount,
			bool withIndex = false,
			bool withDryRun = false,
			bool stopOnError = true,
			CollisionStrategy collisionStrategy = CollisionStrategy.Rename,
			bool withVerification = false,
			bool isFlat = false,
			bool useJsonSidecar = false,
			bool withDelay = false)
		{
			CleanupDir = Path.Combine(Path.GetTempPath(), "BMTP3_E2E_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(CleanupDir);

			Plan = new BackupPlan
			{
				Name = "E2ETest",
				SourceType = BackupSourceType.FileSystem,
				SourcePath = "C:\\E2ESource",
				Destination = CleanupDir,
				Recursive = true,
				DryRun = withDryRun,
				StopOnError = stopOnError,
				BackupIndexType = withIndex ? BackupIndexType.Json : BackupIndexType.None,
				PostWriteVerification = withVerification ? PostWriteVerificationType.Hash : PostWriteVerificationType.None,
				SidecarFormat = useJsonSidecar ? SidecarFormat.Json : SidecarFormat.Ini,
				CollisionStrategy = collisionStrategy,
				CollisionComparisonType = CollisionComparisonType.Binary,
				RenameStrategy = RenameStrategy.Increment,
				ComparisonHashAlgorithmTypes = new[] { HashAlgorithmType.SHA2_256 },
				VerificationHashAlgorithmTypes = withVerification ? new[] { HashAlgorithmType.SHA2_256 } : Array.Empty<HashAlgorithmType>(),
				OutputStructureStrategy = isFlat ? OutputStructureStrategy.Flat : OutputStructureStrategy.PreserveHierarchy,
				ResumeBehavior = SessionResumeStrategy.Abort,
				Delay = withDelay ? 5 : 0,
			};

			BackupItem[] items = Enumerable.Range(0, itemCount)
				.Select(i => CreateItem($"file{i:D3}.jpg", i % 2 == 0 ? "" : "SubDir"))
				.ToArray();

			var drive = new FakeBackupDriveInfo("file:///C:/");
			var traversal = new FakeSourceTraversal();
			var connectedSource = new FakeConnectedSource();
			var scanner = new FakeBackupScanner(items);
			var traversalFactory = new FakeSourceTraversalFactory(traversal);
			var driveProvider = new FakeDriveProvider(new[] { drive });
			var sourceConnector = new FakeSourceConnector(connectedSource);
			var downloadService = new FakeDownloadService();
			var hashService = new FakeHashService();
			var timestampService = new FakeEarliestTimestampResolutionService { FakeTimestamp = FakeTimestamp };
			var sidecarService = new FakeSidecarService();
			var diskSpaceValidator = new FakeDiskSpaceValidator();
			var targetPathResolver = new FakeTargetPathResolver();
			var collisionResolver = new FakeCollisionResolver();
			var backupIndexWriter = new FakeBackupIndexWriter();

			Engine = new BackupEngine(
				scanner, traversalFactory, driveProvider, sourceConnector,
				downloadService, hashService, timestampService,
				sidecarService, diskSpaceValidator,
				NullLogger<BackupEngine>.Instance,
				targetPathResolver, collisionResolver, backupIndexWriter
			);

			DownloadService = downloadService;
			SidecarService = sidecarService;
			BackupIndexWriter = backupIndexWriter;
			CollisionResolver = collisionResolver;
			Progress = new Progress<BackupProgress>(_ => { });
		}

		public void Dispose()
		{
			if(Directory.Exists(CleanupDir))
			{
				try { Directory.Delete(CleanupDir, recursive: true); } catch { /* best effort */ }
			}
		}

		private static BackupItem CreateItem(string fileName, string relativeDir)
		{
			string relativePath = string.IsNullOrEmpty(relativeDir) ? fileName : $"{relativeDir}\\{fileName}";
			return new BackupItem(new FakeMoveableContent($"Content of {fileName}"))
			{
				Id = Guid.NewGuid().ToString("N"),
				SourcePath = @"C:\E2ESource\" + relativePath,
				RelativeFilePath = relativePath,
				FileName = fileName,
				DateCreated = DateTimeOffset.UtcNow.AddDays(-10),
				DateModified = DateTimeOffset.UtcNow.AddDays(-5),
				DateAuthored = DateTimeOffset.UtcNow.AddDays(-7),
				DateAccessed = DateTimeOffset.UtcNow,
			};
		}
	}

	private sealed class SynchronousProgress<T> : IProgress<T>
	{
		private readonly List<T> _target;
		public SynchronousProgress(List<T> target) => _target = target;
		public void Report(T value) => _target.Add(value);
	}

	private sealed class FakeBackupDriveInfo : IBackupFileSystemDriveInfo
	{
		public FakeBackupDriveInfo(string rootPath)
		{
			RootPath = rootPath;
			DriveName = rootPath.TrimEnd('\\');
			Id = rootPath;
			DisplayName = rootPath;
		}
		public string Id { get; }
		public string DriveName { get; }
		public string DisplayName { get; }
		public BackupSourceType SourceType => BackupSourceType.FileSystem;
		public string RootPath { get; }
		public long TotalSize => 1024L * 1024 * 1024 * 1024;
		public long AvailableFreeSpace => 512L * 1024 * 1024 * 1024;
		public string VolumeLabel => DriveName;
		public string DriveFormat => "NTFS";
		public DriveType DriveType => DriveType.Fixed;
	}
}
