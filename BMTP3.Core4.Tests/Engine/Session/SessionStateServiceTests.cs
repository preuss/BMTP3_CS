using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Exceptions;
using BMTP3.Core4.Engine.Session;
using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;
using BMTP3.Core4.State;
using BMTP3.Core4.Tests.Fakes;

namespace BMTP3.Core4.Tests.Engine.Session;

public class SessionStateServiceTests
{
	private static readonly BackupSessionKey TestKey = new("testsession", "FileSystem:C:\\src");

	[Fact]
	public async Task ApplyResumeAsync_NoSummary_DoesNothing()
	{
		FakeSummaryStore store = new(summary: null);
		SessionStateService service = new(store);

		List<BackupRecord> records = CreateRecords(2);
		await service.ApplyResumeAsync(records, TestKey, SessionResumeStrategy.Abort, TestContext.Current.CancellationToken);

		Assert.All(records, r => Assert.Null(r.DestinationPath));
		Assert.All(records, r => Assert.Equal(BackupItemStatus.Pending, r.Status));
	}

	[Fact]
	public async Task ApplyResumeAsync_Abort_SameItems_DoesNothing()
	{
		BackupSummary summary = CreateSummary(2);
		FakeSummaryStore store = new(summary);
		SessionStateService service = new(store);

		List<BackupRecord> records = CreateRecords(2);
		await service.ApplyResumeAsync(records, TestKey, SessionResumeStrategy.Abort, TestContext.Current.CancellationToken);

		Assert.All(records, r => Assert.Equal(BackupItemStatus.Pending, r.Status));
	}

	[Fact]
	public async Task ApplyResumeAsync_Abort_ItemsAdded_Throws()
	{
		BackupSummary summary = CreateSummary(1);
		FakeSummaryStore store = new(summary);
		SessionStateService service = new(store);

		List<BackupRecord> records = CreateRecords(2);
		SessionResumeMismatchException ex = await Assert.ThrowsAsync<SessionResumeMismatchException>(() =>
			service.ApplyResumeAsync(records, TestKey, SessionResumeStrategy.Abort, TestContext.Current.CancellationToken));

		Assert.Equal(1, ex.AddedFiles);
		Assert.Equal(0, ex.RemovedFiles);
	}

	[Fact]
	public async Task ApplyResumeAsync_Abort_ItemsRemoved_Throws()
	{
		BackupSummary summary = CreateSummary(2);
		FakeSummaryStore store = new(summary);
		SessionStateService service = new(store);

		List<BackupRecord> records = CreateRecords(1);
		SessionResumeMismatchException ex = await Assert.ThrowsAsync<SessionResumeMismatchException>(() =>
			service.ApplyResumeAsync(records, TestKey, SessionResumeStrategy.Abort, TestContext.Current.CancellationToken));

		Assert.Equal(0, ex.AddedFiles);
		Assert.Equal(1, ex.RemovedFiles);
	}

	[Fact]
	public async Task ApplyResumeAsync_Restart_DeletesSummary()
	{
		BackupSummary summary = CreateSummary(2);
		FakeSummaryStore store = new(summary);
		SessionStateService service = new(store);

		List<BackupRecord> records = CreateRecords(3);
		await service.ApplyResumeAsync(records, TestKey, SessionResumeStrategy.Restart, TestContext.Current.CancellationToken);

		Assert.True(store.WasDeleted);
	}

	[Fact]
	public async Task ApplyResumeAsync_Continue_MismatchContinues()
	{
		BackupSummary summary = CreateSummary(2);
		FakeSummaryStore store = new(summary);
		SessionStateService service = new(store);

		List<BackupRecord> records = CreateRecords(3);
		await service.ApplyResumeAsync(records, TestKey, SessionResumeStrategy.Continue, TestContext.Current.CancellationToken);

		Assert.False(store.WasDeleted);
	}

	[Fact]
	public async Task ApplyResumeAsync_RestoresCompletedItems()
	{
		List<BackupSummaryItem> items = new()
		{
			new BackupSummaryItem
			{
				Id = "id0",
				SourcePath = "C:\\file0.jpg",
				RelativeFilePath = "file0.jpg",
				FileName = "file0.jpg",
				DestinationPath = "D:\\Backup\\file0.jpg",
				Status = BackupSummaryItemStatus.Succeeded,
				IsCompleted = true,
				CompletedAt = DateTimeOffset.UtcNow.AddHours(-1),
			},
			new BackupSummaryItem
			{
				Id = "id1",
				SourcePath = "C:\\file1.jpg",
				RelativeFilePath = "file1.jpg",
				FileName = "file1.jpg",
				Status = BackupSummaryItemStatus.Pending,
				IsCompleted = false,
			},
		};

		FakeSummaryStore store = new(new BackupSummary
		{
			SessionId = "testsession",
			SourceRoot = "FileSystem:C:\\src",
			CreatedAt = DateTimeOffset.UtcNow,
			Items = items,
		});

		SessionStateService service = new(store);
		List<BackupRecord> records = CreateRecords(2);
		await service.ApplyResumeAsync(records, TestKey, SessionResumeStrategy.Abort, TestContext.Current.CancellationToken);

		Assert.Equal("D:\\Backup\\file0.jpg", records[0].DestinationPath);
		Assert.Equal(BackupItemStatus.Succeeded, records[0].Status);
		Assert.Equal(BackupItemStatus.Pending, records[1].Status);
		Assert.Null(records[1].DestinationPath);
	}

	[Fact]
	public async Task ApplyResumeAsync_CancelledToken_Throws()
	{
		FakeSummaryStore store = new(CreateSummary(2));
		SessionStateService service = new(store);

		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			service.ApplyResumeAsync(CreateRecords(2), TestKey, SessionResumeStrategy.Abort, cts.Token));
	}

	[Fact]
	public async Task SaveAsync_PersistsRecords()
	{
		FakeSummaryStore store = new(summary: null);
		SessionStateService service = new(store);
		List<BackupRecord> records = CreateRecords(2);

		await service.SaveAsync(records, TestKey, TestContext.Current.CancellationToken);

		Assert.NotNull(store.LastSaved);
		Assert.Equal("testsession", store.LastSaved.SessionId);
		Assert.Equal(2, store.LastSaved.Items.Count);
	}

	[Fact]
	public async Task DeleteAsync_DelegatesToStore()
	{
		FakeSummaryStore store = new(CreateSummary(1));
		SessionStateService service = new(store);

		await service.DeleteAsync();

		Assert.True(store.WasDeleted);
	}

	private static List<BackupRecord> CreateRecords(int count)
	{
		return Enumerable.Range(0, count).Select(i => new BackupRecord
		{
			Item = new BackupItem(new FakeContent(new byte[] { 1, 2, 3 }))
			{
				Id = $"id{i}",
				SourcePath = $"C:\\file{i}.jpg",
				RelativeFilePath = $"file{i}.jpg",
				FileName = $"file{i}.jpg",
			},
			SourceDetails = TestSourceDetails,
		}).ToList();
	}

	private static BackupSourceDetails TestSourceDetails => new FileSystemDriveSourceDetails
	{
		DriveName = "C:",
		VolumeLabel = "Test",
		DriveFormat = "NTFS",
	};

	private static BackupSummary CreateSummary(int count)
	{
		return new BackupSummary
		{
			SessionId = "testsession",
			SourceRoot = "FileSystem:C:\\src",
			CreatedAt = DateTimeOffset.UtcNow,
			Items = Enumerable.Range(0, count).Select(i => new BackupSummaryItem
			{
				Id = $"id{i}",
				SourcePath = $"C:\\file{i}.jpg",
				RelativeFilePath = $"file{i}.jpg",
				FileName = $"file{i}.jpg",
			}).ToList(),
		};
	}

	private sealed class FakeSummaryStore : ISummaryStore
	{
		private readonly BackupSummary? _summary;
		public BackupSummary? LastSaved { get; private set; }
		public bool WasDeleted { get; private set; }
		public FileInfo? StoreFile => new("test.json");

		public FakeSummaryStore(BackupSummary? summary)
		{
			_summary = summary;
		}

		public Task<BackupSummary?> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(_summary);
		public Task SaveAsync(BackupSummary summary, CancellationToken ct)
		{
			LastSaved = summary;
			return Task.CompletedTask;
		}
		public Task DeleteAsync()
		{
			WasDeleted = true;
			return Task.CompletedTask;
		}
	}
}
