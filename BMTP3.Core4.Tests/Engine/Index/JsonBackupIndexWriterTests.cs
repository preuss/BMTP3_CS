using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Index;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace BMTP3.Core4.Tests.Engine.Index;

public class JsonBackupIndexWriterTests
{
	[Fact]
	public async Task WriteAsync_CreatesCatalogFile()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_CAT_" + Guid.NewGuid().ToString("N"));
		try
		{
			JsonBackupIndexWriter writer = new(NullLogger<JsonBackupIndexWriter>.Instance);
			List<BackupRecord> records = CreateRecords(2);

			BackupPlan plan = new()
			{
				Name = "TestBackup",
				SourcePath = "C:\\Photos",
				Destination = tempDir,
			};

			BackupResult result = new()
			{
				State = BackupResultState.Completed,
			};

			await writer.WriteAsync(tempDir, "session123", records, plan, result, default);

			string catalogDir = Path.Combine(tempDir, ".bmpt", "session123");
			string catalogFile = Path.Combine(catalogDir, "backup_catalog.json");
			Assert.True(File.Exists(catalogFile));

			string json = File.ReadAllText(catalogFile);
			BackupIndexCatalog? catalog = JsonSerializer.Deserialize<BackupIndexCatalog>(json);
			Assert.NotNull(catalog);
			Assert.Equal("TestBackup", catalog.BackupName);
			Assert.Equal("C:\\Photos", catalog.SourcePath);
			Assert.Equal(2, catalog.TotalFiles);
			Assert.Equal(2, catalog.Files.Count);
		} finally
		{
			if(Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public async Task WriteAsync_EmptyRecords_CreatesCatalogWithZeroFiles()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_CAT_" + Guid.NewGuid().ToString("N"));
		try
		{
			JsonBackupIndexWriter writer = new(NullLogger<JsonBackupIndexWriter>.Instance);
			List<BackupRecord> records = new();

			BackupPlan plan = new() { Name = "Empty", SourcePath = "C:\\", Destination = tempDir };
			BackupResult result = new() { State = BackupResultState.Completed };

			await writer.WriteAsync(tempDir, "session456", records, plan, result, TestContext.Current.CancellationToken);

			string catalogFile = Path.Combine(tempDir, ".bmpt", "session456", "backup_catalog.json");
			string json = File.ReadAllText(catalogFile);
			BackupIndexCatalog? catalog = JsonSerializer.Deserialize<BackupIndexCatalog>(json);

			Assert.NotNull(catalog);
			Assert.Empty(catalog.Files);
			Assert.Equal(0, catalog.TotalFiles);
		} finally
		{
			if(Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public async Task WriteAsync_AtomicWrite_TempFileNotLeftBehind()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_CAT_" + Guid.NewGuid().ToString("N"));
		try
		{
			JsonBackupIndexWriter writer = new(NullLogger<JsonBackupIndexWriter>.Instance);
			List<BackupRecord> records = CreateRecords(1);
			BackupPlan plan = new() { Name = "Atomic", SourcePath = "C:\\", Destination = tempDir };
			BackupResult result = new() { State = BackupResultState.Completed };

			await writer.WriteAsync(tempDir, "session_atomic", records, plan, result, TestContext.Current.CancellationToken);

			string catalogDir = Path.Combine(tempDir, ".bmpt", "session_atomic");
			Assert.False(Directory.EnumerateFiles(catalogDir).Any(f => f.EndsWith(".tmp")), "Temp files should not remain");
		} finally
		{
			if(Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public async Task WriteAsync_CancelledToken_Throws()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_CAT_" + Guid.NewGuid().ToString("N"));
		try
		{
			JsonBackupIndexWriter writer = new(NullLogger<JsonBackupIndexWriter>.Instance);
			using CancellationTokenSource cts = new();
			cts.Cancel();

			await Assert.ThrowsAsync<TaskCanceledException>(() =>
				writer.WriteAsync(tempDir, "session_cancel", CreateRecords(1),
					new BackupPlan { Name = "T", SourcePath = "C:\\", Destination = tempDir },
					new BackupResult { State = BackupResultState.Completed }, cts.Token));
		} finally
		{
			if(Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public async Task WriteAsync_FileWithHashes_IncludesInCatalog()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_CAT_" + Guid.NewGuid().ToString("N"));
		try
		{
			JsonBackupIndexWriter writer = new(NullLogger<JsonBackupIndexWriter>.Instance);

			BackupRecord record = CreateRecord("hashed1");
			record.Metadata.ComputedHashes = new Dictionary<HashType, string>
			{
				[HashType.SHA2_256] = "abcdef",
				[HashType.MD5_128] = "123456",
			};
			record.Status = BackupItemStatus.Succeeded;

			await writer.WriteAsync(tempDir, "session_hash", new[] { record },
				new BackupPlan { Name = "H", SourcePath = "C:\\", Destination = tempDir },
				new BackupResult { State = BackupResultState.Completed }, TestContext.Current.CancellationToken);

			string catalogFile = Path.Combine(tempDir, ".bmpt", "session_hash", "backup_catalog.json");
			string json = File.ReadAllText(catalogFile);
			BackupIndexCatalog? catalog = JsonSerializer.Deserialize<BackupIndexCatalog>(json);

			Assert.NotNull(catalog);
			Assert.NotEmpty(catalog.Files[0].Hashes);
			Assert.Equal("abcdef", catalog.Files[0].Hashes["SHA2_256"]);
		} finally
		{
			if(Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public async Task WriteAsync_FileWithTimestamps_IncludesTimestamps()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_CAT_" + Guid.NewGuid().ToString("N"));
		try
		{
			JsonBackupIndexWriter writer = new(NullLogger<JsonBackupIndexWriter>.Instance);

			BackupRecord record = CreateRecord("ts1");
			record.Metadata.MediaTakenDateTime = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

			await writer.WriteAsync(tempDir, "session_ts", new[] { record },
				new BackupPlan { Name = "TS", SourcePath = "C:\\", Destination = tempDir },
				new BackupResult { State = BackupResultState.Completed }, TestContext.Current.CancellationToken);

			string catalogFile = Path.Combine(tempDir, ".bmpt", "session_ts", "backup_catalog.json");
			string json = File.ReadAllText(catalogFile);
			BackupIndexCatalog? catalog = JsonSerializer.Deserialize<BackupIndexCatalog>(json);

			Assert.NotNull(catalog);
			Assert.NotNull(catalog.Files[0].Timestamps);
		} finally
		{
			if(Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}

	private static List<BackupRecord> CreateRecords(int count)
	{
		return Enumerable.Range(0, count).Select(i => CreateRecord($"id{i:D3}")).ToList();
	}

	private static BackupRecord CreateRecord(string id)
	{
		return new BackupRecord
		{
			Item = new BackupItem(new FakeContent(new byte[] { 1, 2, 3 }))
			{
				Id = id,
				SourcePath = $"C:\\file{id}.jpg",
				RelativeFilePath = $"file{id}.jpg",
				FileName = $"file{id}.jpg",
			},
			DestinationPath = $"D:\\Backup\\file{id}.jpg",
			Status = BackupItemStatus.Succeeded,
			SourceDetails = TestSourceDetails,
		};
	}

	private static readonly BackupSourceDetails TestSourceDetails = new FileSystemDriveSourceDetails
	{
		DriveName = "C:",
		VolumeLabel = "Test",
		DriveFormat = "NTFS",
	};
}
