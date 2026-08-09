using BMTP3.Consoles.IO.Consoles.Progress.Columns;
using Xunit;

namespace BMTP3.Consoles.Tests;

public class FileNameColumnTests
{
	// -------------------------------------------------------------------------
	// TruncateMiddle
	// -------------------------------------------------------------------------

	[Fact]
	public void TruncateMiddle_ShortText_ReturnsAsIs()
	{
		Assert.Equal("hello", FileNameColumn.TruncateMiddle("hello", 10));
	}

	[Fact]
	public void TruncateMiddle_ExactLength_ReturnsAsIs()
	{
		Assert.Equal("hello", FileNameColumn.TruncateMiddle("hello", 5));
	}

	[Fact]
	public void TruncateMiddle_TruncatesFromMiddle()
	{
		// "ABCDEFGHIJ" (10) → max 7 → "ABC...IJ" (8) or "ABC...HIJ" (9) — start+end preserved
		string result = FileNameColumn.TruncateMiddle("ABCDEFGHIJ", 7);
		Assert.Equal(7, result.Length);
		Assert.Contains("...", result);
		Assert.StartsWith("AB", result);
		Assert.EndsWith("IJ", result);
	}

	[Fact]
	public void TruncateMiddle_PreservesStartAndEnd()
	{
		// "ABCDEFGHIJKLMNOPQRSTUVWXYZ" → max 10 → start=ABC, end=XYZ
		string result = FileNameColumn.TruncateMiddle("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 10);
		Assert.Equal(10, result.Length);
		Assert.StartsWith("ABC", result);
		Assert.EndsWith("XYZ", result);
	}

	[Fact]
	public void TruncateMiddle_ResultNeverExceedsMaxLength()
	{
		string input = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
		for (int max = 5; max < input.Length; max++)
		{
			string result = FileNameColumn.TruncateMiddle(input, max);
			Assert.True(result.Length <= max,
				$"maxLength={max}: got '{result}' ({result.Length} chars)");
		}
	}

	// -------------------------------------------------------------------------
	// ShortenPath — no truncation needed
	// -------------------------------------------------------------------------

	[Fact]
	public void ShortenPath_ShortPath_ReturnsAsIs()
	{
		Assert.Equal("photo.txt", FileNameColumn.ShortenPath("photo.txt", 40));
	}

	[Fact]
	public void ShortenPath_ExactLength_ReturnsAsIs()
	{
		string path = new string('a', 36) + ".txt"; // exactly 40 chars
		Assert.Equal(path, FileNameColumn.ShortenPath(path, 40));
	}

	// -------------------------------------------------------------------------
	// ShortenPath — stem truncation (dirs fit, stem does not)
	// -------------------------------------------------------------------------

	[Fact]
	public void ShortenPath_LongStem_TruncatesStemFromMiddle()
	{
		// dirs = "alpha\bravo\" (12 chars), ext = ".txt" (4 chars)
		// stem budget = 40 - 12 - 4 = 24 chars
		string path = @"alpha\bravo\SomeLongFileNameThatDefinitelyDoesNotFitInTheColumn.txt";
		string result = FileNameColumn.ShortenPath(path, 40);

		Assert.True(result.Length <= 40, $"Expected <=40 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".txt", result);
		Assert.Contains("...", result);
		Assert.Contains("alpha", result);
		Assert.Contains("bravo", result);
		Assert.StartsWith(@"alpha\bravo\Some", result);
	}

	[Fact]
	public void ShortenPath_LongStem_UsesFullBudget()
	{
		// Result should be close to maxLength (within 2 chars)
		string path = @"alpha\bravo\SomeLongFileNameThatDefinitelyDoesNotFitInTheColumn.txt";
		string result = FileNameColumn.ShortenPath(path, 40);

		Assert.True(result.Length >= 38, $"Expected >=38 chars, got {result.Length}: '{result}'");
	}

	[Fact]
	public void ShortenPath_LongStem_PreservesEndOfStem()
	{
		// "Column" is at the end of the stem — should be preserved
		string path = @"alpha\bravo\SomeLongFileNameThatDefinitelyDoesNotFitInTheColumn.txt";
		string result = FileNameColumn.ShortenPath(path, 40);

		Assert.Contains("Column", result);
	}

	// -------------------------------------------------------------------------
	// ShortenPath — dir truncation (stem + dirs together too long)
	// -------------------------------------------------------------------------

	[Fact]
	public void ShortenPath_VeryLongDirs_TruncatesDirsFromEnd()
	{
		string path = @"VeryLongDirectoryAlpha\VeryLongDirectoryBravo\short.txt";
		string result = FileNameColumn.ShortenPath(path, 30);

		Assert.True(result.Length <= 30, $"Expected <=30 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".txt", result);
		Assert.Contains("...", result);
	}

	[Fact]
	public void ShortenPath_SingleLongDir_TruncatesDirFromEnd()
	{
		string path = @"VeryLongDirectoryAlpha\short.txt";
		string result = FileNameColumn.ShortenPath(path, 20);

		Assert.True(result.Length <= 20, $"Expected <=20 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".txt", result);
		Assert.Contains("...", result);
	}

	// -------------------------------------------------------------------------
	// ShortenPath — no dirs (filename only)
	// -------------------------------------------------------------------------

	[Fact]
	public void ShortenPath_NoDir_LongStem_TruncatesStemFromMiddle()
	{
		string path = "SomeLongFileNameThatDefinitelyDoesNotFitInTheColumn.txt";
		string result = FileNameColumn.ShortenPath(path, 30);

		Assert.True(result.Length <= 30, $"Expected <=30 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".txt", result);
		Assert.Contains("...", result);
		Assert.StartsWith("SomeLong", result);
	}

	[Fact]
	public void ShortenPath_NoDir_AlwaysPreservesExtension()
	{
		string path = "averylongfilenamethatdoesnotfit.json";
		string result = FileNameColumn.ShortenPath(path, 20);

		Assert.True(result.Length <= 20, $"Expected <=20 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".json", result);
	}

	// -------------------------------------------------------------------------
	// ShortenPath — result never exceeds maxLength
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(@"alpha\bravo\SomeLongFileNameThatDefinitelyDoesNotFitInTheColumn.txt", 40)]
	[InlineData(@"alpha\bravo\SomeLongFileNameThatDefinitelyDoesNotFitInTheColumn.txt", 25)]
	[InlineData(@"alpha\bravo\SomeLongFileNameThatDefinitelyDoesNotFitInTheColumn.txt", 15)]
	[InlineData(@"a\b\c\d\e\f\g\verylongfilename.txt", 20)]
	[InlineData(@"short.txt", 5)]
	[InlineData(@"VeryLongDirectoryAlpha\VeryLongDirectoryBravo\short.txt", 30)]
	// Spaces in dir and file names
	[InlineData(@"My Photos\Summer Holiday\Beach Day Fun.jpg", 40)]
	[InlineData(@"My Photos\Summer Holiday\Beach Day Fun.jpg", 25)]
	[InlineData(@"My Photos\Summer Holiday\Beach Day Fun.jpg", 15)]
	// Deep dir structures
	[InlineData(@"level1\level2\level3\level4\level5\deep file.txt", 40)]
	[InlineData(@"level1\level2\level3\level4\level5\deep file.txt", 20)]
	[InlineData(@"a\b\c\d\e\f\g\h\i\j\file.txt", 15)]
	// Spaces in both dirs and long filename
	[InlineData(@"My Documents\Work Projects\Client Alpha\Report Draft Final Version.docx", 40)]
	[InlineData(@"My Documents\Work Projects\Client Alpha\Report Draft Final Version.docx", 25)]
	public void ShortenPath_ResultNeverExceedsMaxLength(string path, int maxLength)
	{
		string result = FileNameColumn.ShortenPath(path, maxLength);
		Assert.True(result.Length <= maxLength,
			$"maxLength={maxLength}: got '{result}' ({result.Length} chars)");
	}

	// -------------------------------------------------------------------------
	// ShortenPath — extension always preserved
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData(@"alpha\bravo\file.txt", 40, ".txt")]
	[InlineData(@"alpha\bravo\file.txt", 20, ".txt")]
	[InlineData(@"alpha\bravo\file.json", 20, ".json")]
	[InlineData(@"file.txt", 10, ".txt")]
	// Spaces in names
	[InlineData(@"My Photos\Summer Holiday\Beach Day Fun.jpg", 40, ".jpg")]
	[InlineData(@"My Photos\Summer Holiday\Beach Day Fun.jpg", 20, ".jpg")]
	[InlineData(@"My Documents\Work Projects\Report Draft Final Version.docx", 25, ".docx")]
	// Deep dirs
	[InlineData(@"level1\level2\level3\level4\file.txt", 20, ".txt")]
	public void ShortenPath_AlwaysPreservesExtension(string path, int maxLength, string ext)
	{
		string result = FileNameColumn.ShortenPath(path, maxLength);
		Assert.EndsWith(ext, result);
	}

	// -------------------------------------------------------------------------
	// ShortenPath — spaces in dir and file names
	// -------------------------------------------------------------------------

	[Fact]
	public void ShortenPath_SpacesInDirNames_TruncatesCorrectly()
	{
		string path = @"My Photos\Summer Holiday\Beach Day Fun Pic.jpg";
		string result = FileNameColumn.ShortenPath(path, 30);

		Assert.True(result.Length <= 30, $"Expected <=30 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".jpg", result);
		Assert.Contains("...", result);
	}

	[Fact]
	public void ShortenPath_SpacesInFileName_TruncatesStemFromMiddle()
	{
		// dirs fit, long stem with spaces
		string path = @"photos\Long File Name With Many Spaces And Words.jpg";
		string result = FileNameColumn.ShortenPath(path, 35);

		Assert.True(result.Length <= 35, $"Expected <=35 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".jpg", result);
		Assert.Contains("...", result);
		Assert.StartsWith(@"photos\Long", result);
	}

	// -------------------------------------------------------------------------
	// ShortenPath — deep dir structures
	// -------------------------------------------------------------------------

	[Fact]
	public void ShortenPath_DeepDirs_TruncatesFirstDirFirst()
	{
		// 5 levels deep — first dir should be truncated before later dirs
		string path = @"level1\level2\level3\level4\level5\file.txt";
		string result = FileNameColumn.ShortenPath(path, 30);

		Assert.True(result.Length <= 30, $"Expected <=30 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".txt", result);
		Assert.Contains("...", result);
	}

	[Fact]
	public void ShortenPath_DeepDirsWithSpaces_TruncatesCorrectly()
	{
		string path = @"My Documents\Work Projects\Client Alpha\Sub Folder\Report.docx";
		string result = FileNameColumn.ShortenPath(path, 35);

		Assert.True(result.Length <= 35, $"Expected <=35 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".docx", result);
		Assert.Contains("...", result);
	}

	[Fact]
	public void ShortenPath_VeryDeepDirs_StillPreservesExtension()
	{
		string path = @"a\b\c\d\e\f\g\h\i\j\k\l\m\n\o\file.txt";
		string result = FileNameColumn.ShortenPath(path, 15);

		Assert.True(result.Length <= 15, $"Expected <=15 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".txt", result);
	}

	// -------------------------------------------------------------------------
	// ShortenPath — forward slash separator
	// -------------------------------------------------------------------------

	[Fact]
	public void ShortenPath_ForwardSlash_WorksLikeBackslash()
	{
		string path = "alpha/bravo/SomeLongFileNameThatDefinitelyDoesNotFitInTheColumn.txt";
		string result = FileNameColumn.ShortenPath(path, 40);

		Assert.True(result.Length <= 40, $"Expected <=40 chars, got {result.Length}: '{result}'");
		Assert.EndsWith(".txt", result);
		Assert.Contains("...", result);
	}
}
