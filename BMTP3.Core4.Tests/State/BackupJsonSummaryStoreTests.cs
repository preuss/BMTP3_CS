using BMTP3.Core4.Engine.Exceptions;
using BMTP3.Core4.State;
using System.Text.Json;

namespace BMTP3.Core4.Tests.State;

public class BackupJsonSummaryStoreTests : IDisposable
{
	private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_SUM_" + Guid.NewGuid().ToString("N"));

	public BackupJsonSummaryStoreTests()
	{
		Directory.CreateDirectory(_tempDir);
	}

	[Fact]
	public async Task SaveAndLoad_Roundtrip()
	{
		BackupJsonSummaryStore store = new(_tempDir, "abc123def4567");

		BackupSummary summary = new()
		{
			SessionId = "abc123def4567",
			SourceRoot = "FileSystem:C:\\src",
			CreatedAt = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
			Items = new List<BackupSummaryItem>
			{
				new()
				{
					Id = "item1",
					SourcePath = "C:\\file1.jpg",
					RelativeFilePath = "file1.jpg",
					FileName = "file1.jpg",
					Length = 100,
					Status = BackupSummaryItemStatus.Succeeded,
				},
			},
		};

		await store.SaveAsync(summary, TestContext.Current.CancellationToken);
		BackupSummary? loaded = await store.LoadAsync(TestContext.Current.CancellationToken);

		Assert.NotNull(loaded);
		Assert.Equal("abc123def4567", loaded.SessionId);
		Assert.Single(loaded.Items);
		Assert.Equal("item1", loaded.Items[0].Id);
		Assert.Equal(BackupSummaryItemStatus.Succeeded, loaded.Items[0].Status);
	}

	[Fact]
	public async Task SaveAsync_WritesCurrentSessionVersion()
	{
		BackupJsonSummaryStore store = new(_tempDir, "versiontest01");

		await store.SaveAsync(new BackupSummary
		{
			SessionId = "versiontest01",
			SourceRoot = "FileSystem:C:\\src",
			CreatedAt = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
			Items = new List<BackupSummaryItem>(),
		}, TestContext.Current.CancellationToken);

		string json = File.ReadAllText(store.StoreFile!.FullName);
		Assert.Contains("\"SessionVersion\": 2", json);
	}

	[Fact]
	public async Task SaveAsync_GroupsDerivedValuesUnderComputed()
	{
		BackupJsonSummaryStore store = new(_tempDir, "computedtest1");

		await store.SaveAsync(new BackupSummary
		{
			SessionId = "computedtest1",
			SourceRoot = "FileSystem:C:\\src",
			CreatedAt = new DateTimeOffset(2026, 6, 18, 12, 0, 0, TimeSpan.Zero),
			Items = new List<BackupSummaryItem>
			{
				new()
				{
					Id = "item1",
					SourcePath = "C:\\file1.jpg",
					RelativeFilePath = "file1.jpg",
					FileName = "file1.jpg",
					Length = 100,
					Status = BackupSummaryItemStatus.Succeeded,
				},
			},
		}, TestContext.Current.CancellationToken);

		using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(store.StoreFile!.FullName));
		JsonElement root = doc.RootElement;

		// Derived values live only under Computed — never at the top level.
		Assert.False(root.TryGetProperty("TotalFiles", out _));
		Assert.False(root.TryGetProperty("IsComplete", out _));

		// Computed sits above Items so a session file opens with the totals.
		string json = File.ReadAllText(store.StoreFile!.FullName);
		Assert.True(json.IndexOf("\"Computed\"", StringComparison.Ordinal) < json.IndexOf("\"Items\"", StringComparison.Ordinal));

		JsonElement summaryComputed = root.GetProperty("Computed");
		Assert.Equal(1, summaryComputed.GetProperty("TotalFiles").GetInt32());
		Assert.Equal(100, summaryComputed.GetProperty("TotalBytes").GetInt64());
		Assert.Equal(1, summaryComputed.GetProperty("CompletedFiles").GetInt32());
		Assert.Equal(100, summaryComputed.GetProperty("CompletedBytes").GetInt64());
		Assert.True(summaryComputed.GetProperty("IsComplete").GetBoolean());

		JsonElement item = root.GetProperty("Items")[0];
		Assert.False(item.TryGetProperty("IsCompleted", out _));
		Assert.True(item.GetProperty("Computed").GetProperty("IsCompleted").GetBoolean());
	}

	[Fact]
	public async Task LoadAsync_LegacySessionWithoutVersion_Throws()
	{
		BackupJsonSummaryStore store = new(_tempDir, "legacyses001");
		File.WriteAllText(store.StoreFile!.FullName, """
		{
		  "SessionId": "legacyses001",
		  "SourceRoot": "FileSystem:C:\\src",
		  "CreatedAt": "2026-06-18T12:00:00+00:00",
		  "Items": []
		}
		""");

		SessionVersionMismatchException ex = await Assert.ThrowsAsync<SessionVersionMismatchException>(
			() => store.LoadAsync(TestContext.Current.CancellationToken));
		Assert.Equal(1, ex.FoundVersion);
		Assert.Equal(BackupSummary.CurrentVersion, ex.ExpectedVersion);
	}

	[Fact]
	public async Task LoadAsync_LegacyFileWithFlatDerivedMembers_ThrowsVersionMismatch()
	{
		// Pre-versioning sessions also carried the flat derived members at root and on items —
		// they must deserialize without mapping errors and still fail on the version check.
		BackupJsonSummaryStore store = new(_tempDir, "legacyflat01");
		File.WriteAllText(store.StoreFile!.FullName, """
		{
		  "SessionId": "legacyflat01",
		  "SourceRoot": "FileSystem:C:\\src",
		  "CreatedAt": "2026-06-18T12:00:00+00:00",
		  "TotalFiles": 1,
		  "TotalBytes": 100,
		  "CompletedFiles": 1,
		  "CompletedBytes": 100,
		  "IsComplete": true,
		  "Items": [
		    {
		      "Id": "item1",
		      "SourcePath": "C:\\file1.jpg",
		      "RelativeFilePath": "file1.jpg",
		      "FileName": "file1.jpg",
		      "Length": 100,
		      "Status": 1,
		      "IsCompleted": true
		    }
		  ]
		}
		""");

		SessionVersionMismatchException ex = await Assert.ThrowsAsync<SessionVersionMismatchException>(
			() => store.LoadAsync(TestContext.Current.CancellationToken));
		Assert.Equal(1, ex.FoundVersion);
		Assert.Equal(BackupSummary.CurrentVersion, ex.ExpectedVersion);
	}

	[Fact]
	public async Task LoadAsync_WrongSessionVersion_Throws()
	{
		BackupJsonSummaryStore store = new(_tempDir, "futurevers001");
		File.WriteAllText(store.StoreFile!.FullName, """
		{
		  "SessionVersion": 99,
		  "SessionId": "futurevers001",
		  "SourceRoot": "FileSystem:C:\\src",
		  "CreatedAt": "2026-06-18T12:00:00+00:00",
		  "Items": []
		}
		""");

		SessionVersionMismatchException ex = await Assert.ThrowsAsync<SessionVersionMismatchException>(
			() => store.LoadAsync(TestContext.Current.CancellationToken));
		Assert.Equal(99, ex.FoundVersion);
	}

	[Fact]
	public async Task LoadAsync_NoFile_ReturnsNull()
	{
		BackupJsonSummaryStore store = new(_tempDir, "nonexistent12");
		BackupSummary? loaded = await store.LoadAsync(TestContext.Current.CancellationToken);
		Assert.Null(loaded);
	}

	[Fact]
	public async Task DeleteAsync_RemovesFile()
	{
		BackupJsonSummaryStore store = new(_tempDir, "deletableid123");
		await store.SaveAsync(new BackupSummary
		{
			SessionId = "deletableid123",
			SourceRoot = "C:\\",
			CreatedAt = DateTimeOffset.UtcNow,
			Items = new List<BackupSummaryItem>(),
		}, TestContext.Current.CancellationToken);

		Assert.True(File.Exists(Path.Combine(_tempDir, "session_deletableid1.json")));

		await store.DeleteAsync();
		Assert.False(File.Exists(Path.Combine(_tempDir, "session_deletableid1.json")));
	}

	[Fact]
	public async Task DeleteAsync_NoFile_DoesNotThrow()
	{
		BackupJsonSummaryStore store = new(_tempDir, "absentid12345");
		await store.DeleteAsync();
	}

	[Fact]
	public void StoreFile_ReturnsCorrectPath()
	{
		BackupJsonSummaryStore store = new(_tempDir, "mysessionid123");
		Assert.NotNull(store.StoreFile);
		Assert.Equal(
			Path.Combine(_tempDir, "session_mysessionid1.json"),
			store.StoreFile.FullName);
	}

	[Fact]
	public void Constructor_NullDirectory_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new BackupJsonSummaryStore(null!, "sp"));
	}

	[Fact]
	public void Constructor_NullSessionId_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => new BackupJsonSummaryStore(_tempDir, null!));
	}

	[Fact]
	public async Task SaveAsync_AtomicWrite_NoTempFileLeft()
	{
		BackupJsonSummaryStore store = new(_tempDir, "atomicid12345");

		await store.SaveAsync(new BackupSummary
		{
			SessionId = "atomicid12345",
			SourceRoot = "C:\\",
			CreatedAt = DateTimeOffset.UtcNow,
			Items = new List<BackupSummaryItem>(),
		}, TestContext.Current.CancellationToken);

		Assert.DoesNotContain(Directory.EnumerateFiles(_tempDir), f => f.EndsWith(".tmp"));
	}

	public void Dispose()
	{
		if(Directory.Exists(_tempDir))
			try { Directory.Delete(_tempDir, recursive: true); } catch { }
	}
}
