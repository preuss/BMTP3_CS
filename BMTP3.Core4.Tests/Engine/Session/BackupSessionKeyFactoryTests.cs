using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Session;

namespace BMTP3.Core4.Tests.Engine.Session;

public class BackupSessionKeyFactoryTests
{
	[Fact]
	public void Create_WithValidPlan_ReturnsKey()
	{
		BackupSessionKey key = BackupSessionKeyFactory.Create(
			"C:\\MyPhotos", "D:\\Backup", BackupSourceType.FileSystem);

		Assert.NotNull(key.SessionId);
		Assert.NotNull(key.SourceIdentity);
		Assert.Equal("C:\\MyPhotos:D:\\Backup:FileSystem", key.SourceIdentity);
	}

	[Fact]
	public void Create_SameSourceAndDestination_ReturnsSameSessionId()
	{
		BackupSessionKey keyA = BackupSessionKeyFactory.Create(
			"C:\\MyPhotos", "D:\\Backup", BackupSourceType.FileSystem);
		BackupSessionKey keyB = BackupSessionKeyFactory.Create(
			"C:\\MyPhotos", "D:\\Backup", BackupSourceType.FileSystem);

		Assert.Equal(keyA.SessionId, keyB.SessionId);
		Assert.Equal(keyA.SourceIdentity, keyB.SourceIdentity);
	}

	[Fact]
	public void Create_DifferentDestination_DifferentSessionId()
	{
		BackupSessionKey keyA = BackupSessionKeyFactory.Create(
			"C:\\MyPhotos", "D:\\Backup", BackupSourceType.FileSystem);
		BackupSessionKey keyB = BackupSessionKeyFactory.Create(
			"C:\\MyPhotos", "E:\\Other", BackupSourceType.FileSystem);

		Assert.NotEqual(keyA.SessionId, keyB.SessionId);
		Assert.NotEqual(keyA.SourceIdentity, keyB.SourceIdentity);
	}

	[Fact]
	public void Create_DifferentSourceType_DifferentSessionId()
	{
		BackupSessionKey keyFs = BackupSessionKeyFactory.Create(
			"C:\\MyPhotos", "D:\\Backup", BackupSourceType.FileSystem);
		BackupSessionKey keyMtp = BackupSessionKeyFactory.Create(
			"C:\\MyPhotos", "D:\\Backup", BackupSourceType.MediaDevice);

		Assert.NotEqual(keyFs.SessionId, keyMtp.SessionId);
	}

	[Fact]
	public void Create_DifferentSourcePath_DifferentSessionId()
	{
		Assert.NotEqual(
			BackupSessionKeyFactory.Create("C:\\A", "D:\\Backup", BackupSourceType.FileSystem).SessionId,
			BackupSessionKeyFactory.Create("C:\\B", "D:\\Backup", BackupSourceType.FileSystem).SessionId
		);
	}

	[Fact]
	public void Create_SessionId_IsHexString()
	{
		string sessionId = BackupSessionKeyFactory.Create(
			"C:\\Test", "D:\\Backup", BackupSourceType.FileSystem).SessionId;

		Assert.Matches("^[0-9a-f]{64}$", sessionId);
	}

	[Fact]
	public void Create_SourcePathTrimmed_IgnoresSpaces()
	{
		Assert.Equal(
			BackupSessionKeyFactory.Create("  C:\\Path  ", "D:\\Backup", BackupSourceType.FileSystem).SessionId,
			BackupSessionKeyFactory.Create("C:\\Path", "D:\\Backup", BackupSourceType.FileSystem).SessionId
		);
	}

	[Fact]
	public void Create_NullSourcePath_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => BackupSessionKeyFactory.Create(
			null!, "D:\\Backup", BackupSourceType.FileSystem));
	}

	[Fact]
	public void Create_NullDestination_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => BackupSessionKeyFactory.Create(
			"C:\\Path", null!, BackupSourceType.FileSystem));
	}
}
