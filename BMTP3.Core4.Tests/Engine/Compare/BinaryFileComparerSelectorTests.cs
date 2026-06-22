using BMTP3.Core4.Engine.Compare;
using BMTP3.Core4.Engine.Compare.Algorithms;

namespace BMTP3.Core4.Tests.Engine.Compare;

public class BinaryFileComparerSelectorTests : IDisposable
{
	private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_BFC_" + Guid.NewGuid().ToString("N"));

	private readonly WholeFileSequenceEqualBinaryComparer _wholeFile = new();
	private readonly ChunkedSequenceEqualBinaryComparer _chunkedDefault = new();
	private readonly ChunkedVectorBinaryComparer _chunkedVector = new();
	private readonly ChunkedEightByteBinaryComparer _chunkedEightByte = new();
	private readonly ChunkedAvx2BinaryComparer _chunkedAvx2 = new();

	private readonly BinaryFileComparerSelector _selector;

	public BinaryFileComparerSelectorTests()
	{
		Directory.CreateDirectory(_tempDir);
		_selector = new BinaryFileComparerSelector(
			_wholeFile,
			_chunkedDefault,
			_chunkedVector,
			_chunkedEightByte,
			_chunkedAvx2);
	}

	public void Dispose()
	{
		if (Directory.Exists(_tempDir))
			Directory.Delete(_tempDir, recursive: true);
	}

	// -----------------------------------------------------------------------
	// Constructor null guards
	// -----------------------------------------------------------------------

	[Fact]
	public void Constructor_NullWholeFile_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new BinaryFileComparerSelector(null!, _chunkedDefault, _chunkedVector,
				_chunkedEightByte, _chunkedAvx2));
	}

	[Fact]
	public void Constructor_NullChunkedDefault_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new BinaryFileComparerSelector(_wholeFile, null!, _chunkedVector,
				_chunkedEightByte, _chunkedAvx2));
	}

	[Fact]
	public void Constructor_NullChunkedVector_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new BinaryFileComparerSelector(_wholeFile, _chunkedDefault, null!,
				_chunkedEightByte, _chunkedAvx2));
	}

	[Fact]
	public void Constructor_NullChunkedEightByte_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new BinaryFileComparerSelector(_wholeFile, _chunkedDefault, _chunkedVector,
				null!, _chunkedAvx2));
	}

	[Fact]
	public void Constructor_NullChunkedAvx2_Throws()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new BinaryFileComparerSelector(_wholeFile, _chunkedDefault, _chunkedVector,
				_chunkedEightByte, null!));
	}

	// -----------------------------------------------------------------------
	// Select null/argument guards
	// -----------------------------------------------------------------------

	[Fact]
	public void Select_NullSourceInfo_Throws()
	{
		FileInfo target = new(Path.Combine(_tempDir, "target.txt"));
		File.WriteAllText(target.FullName, "content");

		Assert.Throws<ArgumentNullException>(() => _selector.Select(null!, target));
	}

	[Fact]
	public void Select_NullTargetInfo_Throws()
	{
		FileInfo source = new(Path.Combine(_tempDir, "source.txt"));
		File.WriteAllText(source.FullName, "content");

		Assert.Throws<ArgumentNullException>(() => _selector.Select(source, null!));
	}

	[Fact]
	public void Select_NonExistentSource_Throws()
	{
		FileInfo source = new(Path.Combine(_tempDir, "missing_source.txt"));
		FileInfo target = new(Path.Combine(_tempDir, "target.txt"));
		File.WriteAllText(target.FullName, "content");

		ArgumentException ex = Assert.Throws<ArgumentException>(
			() => _selector.Select(source, target));
		Assert.Contains("sourceInfo", ex.ParamName ?? "");
	}

	[Fact]
	public void Select_NonExistentTarget_Throws()
	{
		FileInfo source = new(Path.Combine(_tempDir, "source.txt"));
		File.WriteAllText(source.FullName, "content");
		FileInfo target = new(Path.Combine(_tempDir, "missing_target.txt"));

		ArgumentException ex = Assert.Throws<ArgumentException>(
			() => _selector.Select(source, target));
		Assert.Contains("targetInfo", ex.ParamName ?? "");
	}

	// -----------------------------------------------------------------------
	// Small files (<= 10 MB) → WholeFileSequenceEqualBinaryComparer
	// -----------------------------------------------------------------------

	[Fact]
	public void Select_BothSmallFiles_ReturnsWholeFile()
	{
		FileInfo source = CreateFile("source_small.bin", 1024);
		FileInfo target = CreateFile("target_small.bin", 512);

		IBinaryFileComparer result = _selector.Select(source, target);

		Assert.Same(_wholeFile, result);
	}

	[Fact]
	public void Select_OneAtThresholdOneSmaller_ReturnsWholeFile()
	{
		FileInfo source = CreateFile("source_threshold.bin", 10 * 1024 * 1024);
		FileInfo target = CreateFile("target_smaller.bin", 1024);

		IBinaryFileComparer result = _selector.Select(source, target);

		Assert.Same(_wholeFile, result);
	}

	// -----------------------------------------------------------------------
	// Large files (> 10 MB) → chunked comparer
	// -----------------------------------------------------------------------

	[Fact]
	public void Select_BothLargeFiles_ReturnsChunkedComparer()
	{
		// Use sparse files — instant allocation on NTFS
		long size = 11L * 1024 * 1024;
		FileInfo source = CreateSparseFile("source_large.bin", size);
		FileInfo target = CreateSparseFile("target_large.bin", size);

		IBinaryFileComparer result = _selector.Select(source, target);

		Assert.IsAssignableFrom<IBinaryFileComparer>(result);
		// Must not be WholeFile
		Assert.NotSame(_wholeFile, result);
	}

	[Fact]
	public void Select_OneLargeOneSmall_ReturnsChunkedComparer()
	{
		long size = 11L * 1024 * 1024;
		FileInfo source = CreateSparseFile("source_mixed.bin", size);
		FileInfo target = CreateFile("target_small.bin", 1024);

		IBinaryFileComparer result = _selector.Select(source, target);

		Assert.IsAssignableFrom<IBinaryFileComparer>(result);
		Assert.NotSame(_wholeFile, result);
	}

	// -----------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------

	private FileInfo CreateFile(string name, long sizeBytes)
	{
		string path = Path.Combine(_tempDir, name);
		byte[] data = new byte[sizeBytes];
		new Random(42).NextBytes(data);
		File.WriteAllBytes(path, data);
		return new FileInfo(path);
	}

	private FileInfo CreateSparseFile(string name, long sizeBytes)
	{
		string path = Path.Combine(_tempDir, name);
		using (FileStream fs = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
		{
			fs.SetLength(sizeBytes);
		}
		return new FileInfo(path);
	}
}
