using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Session;

namespace BMTP3.Core4.Tests.Engine.Session;

public class BackupSessionKeyFactoryTests
{
	[Fact]
	public void Create_WithValidPlan_ReturnsKey()
	{
		BackupPlan plan = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "C:\\MyPhotos",
			Destination = "D:\\Backup",
			Name = "Test",
		};

		BackupSessionKey key = BackupSessionKeyFactory.Create(plan);

		Assert.NotNull(key.SessionId);
		Assert.NotNull(key.SourceIdentity);
		Assert.Equal("FileSystem:C:\\MyPhotos", key.SourceIdentity);
	}

	[Fact]
	public void Create_SamePlan_ReturnsSameSessionId()
	{
		BackupPlan planA = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "C:\\MyPhotos",
			Destination = "D:\\Backup",
			Name = "Test",
		};

		BackupPlan planB = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "C:\\MyPhotos",
			Destination = "E:\\Other",
			Name = "Different",
		};

		BackupSessionKey keyA = BackupSessionKeyFactory.Create(planA);
		BackupSessionKey keyB = BackupSessionKeyFactory.Create(planB);

		Assert.Equal(keyA.SessionId, keyB.SessionId);
		Assert.Equal(keyA.SourceIdentity, keyB.SourceIdentity);
	}

	[Fact]
	public void Create_DifferentSourceType_DifferentSessionId()
	{
		BackupPlan planFs = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "C:\\MyPhotos",
			Destination = "D:\\Backup",
		};

		BackupPlan planMtp = new()
		{
			SourceType = BackupSourceType.MediaDevice,
			SourcePath = "C:\\MyPhotos",
			Destination = "D:\\Backup",
		};

		BackupSessionKey keyFs = BackupSessionKeyFactory.Create(planFs);
		BackupSessionKey keyMtp = BackupSessionKeyFactory.Create(planMtp);

		Assert.NotEqual(keyFs.SessionId, keyMtp.SessionId);
	}

	[Fact]
	public void Create_DifferentSourcePath_DifferentSessionId()
	{
		BackupPlan planA = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "C:\\A",
			Destination = "D:\\Backup",
		};

		BackupPlan planB = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "C:\\B",
			Destination = "D:\\Backup",
		};

		Assert.NotEqual(
			BackupSessionKeyFactory.Create(planA).SessionId,
			BackupSessionKeyFactory.Create(planB).SessionId
		);
	}

	[Fact]
	public void Create_SessionId_IsHexString()
	{
		BackupPlan plan = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "C:\\Test",
			Destination = "D:\\Backup",
		};

		string sessionId = BackupSessionKeyFactory.Create(plan).SessionId;

		Assert.Matches("^[0-9a-f]{64}$", sessionId);
	}

	[Fact]
	public void Create_SourcePathTrimmed_IgnoresSpaces()
	{
		BackupPlan planUntrimmed = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "  C:\\Path  ",
			Destination = "D:\\Backup",
		};

		BackupPlan planTrimmed = new()
		{
			SourceType = BackupSourceType.FileSystem,
			SourcePath = "C:\\Path",
			Destination = "D:\\Backup",
		};

		Assert.Equal(
			BackupSessionKeyFactory.Create(planUntrimmed).SessionId,
			BackupSessionKeyFactory.Create(planTrimmed).SessionId
		);
	}

	[Fact]
	public void Create_NullPlan_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => BackupSessionKeyFactory.Create(null!));
	}
}
