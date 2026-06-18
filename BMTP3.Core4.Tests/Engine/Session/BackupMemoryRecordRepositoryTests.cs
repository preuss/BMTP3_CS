using BMTP3.Core4.Engine.Session;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Tests.Engine.Session;

public class BackupMemoryRecordRepositoryTests
{
	[Fact]
	public void Add_ThenGetAll_ReturnsRecords()
	{
		BackupMemoryRecordRepository repo = new();
		BackupRecord record = CreateRecord("id1");

		repo.Add(record);
		IReadOnlyList<BackupRecord> all = repo.GetAll();

		Assert.Single(all);
		Assert.Same(record, all[0]);
	}

	[Fact]
	public void Add_MultipleRecords_ReturnsAll()
	{
		BackupMemoryRecordRepository repo = new();
		repo.Add(CreateRecord("a"));
		repo.Add(CreateRecord("b"));
		repo.Add(CreateRecord("c"));

		Assert.Equal(3, repo.GetAll().Count);
	}

	[Fact]
	public void GetAll_EmptyRepository_ReturnsEmpty()
	{
		BackupMemoryRecordRepository repo = new();
		Assert.Empty(repo.GetAll());
	}

	[Fact]
	public void GetAll_ReturnsNewListEachCall()
	{
		BackupMemoryRecordRepository repo = new();
		repo.Add(CreateRecord("x"));

		IReadOnlyList<BackupRecord> first = repo.GetAll();
		IReadOnlyList<BackupRecord> second = repo.GetAll();

		Assert.NotSame(first, second);
	}

	private static BackupRecord CreateRecord(string id)
	{
		return new BackupRecord
		{
			Item = new BackupItem(new Fakes.FakeContent(new byte[] { 1, 2, 3 }))
			{
				Id = id,
				SourcePath = $"C:\\{id}.jpg",
				RelativeFilePath = $"{id}.jpg",
				FileName = $"{id}.jpg",
			},
			SourceDetails = new FileSystemDriveSourceDetails
			{
				DriveName = "C:",
				VolumeLabel = "Test",
				DriveFormat = "NTFS",
			},
		};
	}
}
