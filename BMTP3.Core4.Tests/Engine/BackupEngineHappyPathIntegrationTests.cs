using BMTP3.Core4.Api;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine;
using BMTP3.Core4.Models;
using BMTP3.Core4.Storage;
using BMTP3.Core4.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core4.Tests.Engine;

public class BackupEngineHappyPathIntegrationTests
{
	[Fact]
	public async Task RunAsync_WithAllFakes_HappyPath_AllItemsSucceed()
	{
		string testDir = Path.Combine(Path.GetTempPath(), "BMTP3_Test_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(testDir);
			string sourceRoot = Path.Combine(testDir, "Source");
			string driveRoot = Path.GetPathRoot(testDir) ?? "C:\\";

			BackupPlan plan = CreateTestPlan(sourceRoot, testDir);

			FakeBackupDriveInfo drive = new(driveRoot, driveRoot.TrimEnd('\\'));
			FakeSourceTraversal traversal = new();
			FakeConnectedSource connectedSource = new();

			BackupItem item1 = CreateBackupItem("photo.jpg", "", sourceRoot);
			BackupItem item2 = CreateBackupItem("document.pdf", "Docs", sourceRoot);

			FakeBackupScanner scanner = new(new[] { item1, item2 });
			FakeSourceTraversalFactory traversalFactory = new(traversal);
			FakeDriveProvider driveProvider = new(new[] { drive });
			FakeSourceConnector sourceConnector = new(connectedSource);
			FakeDownloadService downloadService = new();
			FakeHashService hashService = new();
			FakeEarliestTimestampResolutionService timestampService = new()
			{
				FakeTimestamp = DateTimeOffset.UtcNow.AddDays(-7),
			};
			FakeSidecarService sidecarService = new();
			FakeDiskSpaceValidator diskSpaceValidator = new();
			FakeTargetPathResolver targetPathResolver = new();
			FakeCollisionResolver collisionResolver = new();
			FakeBackupIndexWriter backupIndexWriter = new();

			BackupEngine engine = new(
				scanner,
				traversalFactory,
				driveProvider,
				sourceConnector,
				downloadService,
				hashService,
				timestampService,
				sidecarService,
				diskSpaceValidator,
				NullLogger<BackupEngine>.Instance,
				targetPathResolver,
				collisionResolver,
				backupIndexWriter
			);

			List<BackupProgress> progressReports = new();

			BackupResult result = await engine.RunAsync(plan, new Progress<BackupProgress>(p => progressReports.Add(p)), CancellationToken.None);

			Assert.Equal(BackupResultState.Completed, result.State);
			Assert.Equal(2, result.ItemResults.Count);
			Assert.All(result.ItemResults, r => Assert.Equal(BackupResultItemState.Succeeded, r.State));

			Assert.Equal(2, sidecarService.WriteCount);
			Assert.Equal(0, backupIndexWriter.WriteCount);

			Assert.Contains(progressReports, p => p.CurrentPhase == BackupProgressPhase.Starting);
			Assert.Contains(progressReports, p => p.CurrentPhase == BackupProgressPhase.Transferring);
			Assert.Contains(progressReports, p => p.CurrentPhase == BackupProgressPhase.Completed);
			Assert.Equal(2, progressReports.Last().FilesSucceeded);

			Assert.True(Directory.Exists(Path.Combine(testDir, ".bmtp3")));
		}
		finally
		{
			if (Directory.Exists(testDir))
				Directory.Delete(testDir, recursive: true);
		}
	}

	private static BackupPlan CreateTestPlan(string sourcePath, string destination)
	{
		return new BackupPlan
		{
			Name = "IntegrationTest",
			SourceType = BackupSourceType.FileSystem,
			SourcePath = sourcePath,
			Destination = destination,
			Recursive = true,
			DryRun = false,
			StopOnError = true,
			EnableMetadata = false,
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

	private sealed class FakeBackupDriveInfo : IBackupDriveInfo
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
		public long TotalSize => 1024 * 1024 * 1024;
		public long AvailableFreeSpace => 512 * 1024 * 1024;
	}
}
