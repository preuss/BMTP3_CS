using BMTP3.Core4.Engine;
using BMTP3.Core4.Engine.Session;

namespace BMTP3.Core4.Tests.Engine;

public class TempDirectoryHelperTests
{
	private static readonly BackupSessionKey TestKey = new("abc123session", "FileSystem:C:\\src");
	private static readonly DateTimeOffset TestTime = new(2026, 6, 18, 14, 30, 0, TimeSpan.Zero);

	[Fact]
	public void ResolveTempDirectoryPath_ReturnsBmtp3Path()
	{
		DirectoryInfo dir = TempDirectoryHelper.ResolveTempDirectoryPath("D:\\Backup", TestTime, TestKey);

		Assert.StartsWith("D:\\Backup\\.tmp", dir.FullName);
		Assert.EndsWith("abc123session", dir.Name);
	}

	[Fact]
	public void ResolveTempDirectoryPath_IncludesTimestamp()
	{
		DirectoryInfo dir = TempDirectoryHelper.ResolveTempDirectoryPath("D:\\Backup", TestTime, TestKey);
		string name = dir.Name;

		Assert.StartsWith("20260618_143000", name);
	}

	[Fact]
	public void ResolveTempDirectoryPath_NullDestination_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			TempDirectoryHelper.ResolveTempDirectoryPath(null!, TestTime, TestKey));
	}

	[Fact]
	public void ResolveTempDirectoryPath_NullSessionKey_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			TempDirectoryHelper.ResolveTempDirectoryPath("D:\\Backup", TestTime, null!));
	}

	[Fact]
	public void PrepareTempDirectory_CreatesDirectory()
	{
		string tempPath = Path.Combine(Path.GetTempPath(), "BMTP3_TMP_" + Guid.NewGuid().ToString("N"));
		try
		{
			DirectoryInfo dir = new(tempPath);
			Assert.False(dir.Exists);

			TempDirectoryHelper.PrepareTempDirectory(dir);
			Assert.True(dir.Exists);

			TempDirectoryHelper.PrepareTempDirectory(dir);
			Assert.True(dir.Exists);
		} finally
		{
			if(Directory.Exists(tempPath))
				Directory.Delete(tempPath);
		}
	}

	[Fact]
	public void PrepareTempDirectory_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => TempDirectoryHelper.PrepareTempDirectory(null!));
	}

	[Fact]
	public void BuildTempFileName_ReturnsGuidPlusSanitizedName()
	{
		string name = TempDirectoryHelper.BuildTempFileName("photo.jpg");

		Assert.Contains("photo", name);
		Assert.Matches(@"^[0-9a-f]{32}_", name);
	}

	[Fact]
	public void BuildTempFileName_SanitizesInvalidChars()
	{
		string name = TempDirectoryHelper.BuildTempFileName("bad<file>:name?.txt");

		Assert.Contains("bad_file_name_.txt", name);
		Assert.DoesNotContain("<", name);
		Assert.DoesNotContain(":", name);
		Assert.DoesNotContain("?", name);
	}

	[Fact]
	public void BuildTempFileName_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => TempDirectoryHelper.BuildTempFileName(null!));
	}

	[Fact]
	public void BuildTempFileName_ReservedDeviceName_AppendsSuffix()
	{
		string name = TempDirectoryHelper.BuildTempFileName("CON");

		Assert.Contains("CON_file", name);
	}

	[Fact]
	public void BuildTempFileName_LongName_Truncated()
	{
		string longName = new string('a', 300) + ".txt";
		string name = TempDirectoryHelper.BuildTempFileName(longName);

		Assert.True(name.Length <= 200, $"Expected <= 200, got {name.Length}");
		Assert.Contains(".txt", name);
	}

	[Fact]
	public void BuildTempFilePath_CombinesDirAndFileName()
	{
		DirectoryInfo dir = new("D:\\Backup\\.tmp\\session");
		FileInfo file = TempDirectoryHelper.BuildTempFilePath(dir, "test.jpg");

		Assert.StartsWith(dir.FullName, file.FullName);
		Assert.Contains("test", file.Name);
	}

	[Fact]
	public void CleanupSessionTempDirectory_NonExistent_ReturnsTrue()
	{
		DirectoryInfo dir = new("Z:\\nonexistent\\path\\20260618_143000_abc123");
		bool result = TempDirectoryHelper.CleanupSessionTempDirectory(dir);
		Assert.True(result);
	}

	[Fact]
	public void CleanupSessionTempDirectory_InvalidPattern_Throws()
	{
		DirectoryInfo dir = new("C:\\Windows");
		Assert.Throws<InvalidOperationException>(() =>
			TempDirectoryHelper.CleanupSessionTempDirectory(dir));
	}

	[Fact]
	public void CleanupSessionTempDirectory_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			TempDirectoryHelper.CleanupSessionTempDirectory(null!));
	}

	[Fact]
	public void CleanupTempFiles_RemovesFiles()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_CLN_" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(tempDir);
			string file1 = Path.Combine(tempDir, "f1.tmp");
			string file2 = Path.Combine(tempDir, "f2.tmp");
			File.WriteAllText(file1, "a");
			File.WriteAllText(file2, "b");

			TempDirectoryHelper.CleanupTempFiles(file1, file2);

			Assert.False(File.Exists(file1));
			Assert.False(File.Exists(file2));
		} finally
		{
			if(Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}

	[Fact]
	public void CleanupTempFiles_NullPaths_DoesNotThrow()
	{
		TempDirectoryHelper.CleanupTempFiles(null, null);
		TempDirectoryHelper.CleanupTempFiles("Z:\\nonexistent\\file.tmp", null);
	}

	[Fact]
	public void CleanupSessionTempDirectory_EmptyValidDir_RemovesIt()
	{
		string tempRoot = Path.Combine(Path.GetTempPath(), "BMTP3_CLN_" + Guid.NewGuid().ToString("N"));
		try
		{
			DirectoryInfo sessionDir = TempDirectoryHelper.ResolveTempDirectoryPath(tempRoot, TestTime, TestKey);
			TempDirectoryHelper.PrepareTempDirectory(sessionDir);

			Assert.True(sessionDir.Exists);

			bool removed = TempDirectoryHelper.CleanupSessionTempDirectory(sessionDir);
			Assert.True(removed);
			sessionDir.Refresh();
			Assert.False(sessionDir.Exists);
		} finally
		{
			if(Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void CleanupSessionTempDirectory_RemovesParentIfEmpty()
	{
		string tempRoot = Path.Combine(Path.GetTempPath(), "BMTP3_CLN_" + Guid.NewGuid().ToString("N"));
		try
		{
			DirectoryInfo sessionDir = TempDirectoryHelper.ResolveTempDirectoryPath(tempRoot, TestTime, TestKey);
			TempDirectoryHelper.PrepareTempDirectory(sessionDir);

			TempDirectoryHelper.CleanupSessionTempDirectory(sessionDir);

			DirectoryInfo? tmpRoot = new(Path.Combine(tempRoot, ".tmp"));
			tmpRoot.Refresh();
			Assert.False(tmpRoot.Exists);
		} finally
		{
			if(Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, recursive: true);
		}
	}

	[Fact]
	public void CleanupSessionTempDirectory_NonEmptyDir_NotRemoved()
	{
		string tempRoot = Path.Combine(Path.GetTempPath(), "BMTP3_CLN_" + Guid.NewGuid().ToString("N"));
		try
		{
			DirectoryInfo sessionDir = TempDirectoryHelper.ResolveTempDirectoryPath(tempRoot, TestTime, TestKey);
			TempDirectoryHelper.PrepareTempDirectory(sessionDir);

			File.WriteAllText(Path.Combine(sessionDir.FullName, "orphan.tmp"), "data");

			bool removed = TempDirectoryHelper.CleanupSessionTempDirectory(sessionDir);
			Assert.False(removed);
			Assert.True(sessionDir.Exists);
		} finally
		{
			if(Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, recursive: true);
		}
	}
}
