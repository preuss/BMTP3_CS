using BMTP3.Core4.Engine.Compare;
using BMTP3.Core4.Engine.Compare.Algorithms;

namespace BMTP3.Core4.Tests.Engine.Compare;

public class FileCompareServiceTests : IDisposable
{
	private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "BMTP3_CMP_" + Guid.NewGuid().ToString("N"));

	public FileCompareServiceTests()
	{
		Directory.CreateDirectory(_tempDir);
	}

	[Fact]
	public async Task CompareAsync_IdenticalFiles_ReturnsTrue()
	{
		string src = CreateFile("src.txt", "Hello, World!");
		string dst = CreateFile("dst.txt", "Hello, World!");
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		bool result = await service.CompareAsync(src, dst, TestContext.Current.CancellationToken);

		Assert.True(result);
	}

	[Fact]
	public async Task CompareAsync_DifferentFiles_ReturnsFalse()
	{
		string src = CreateFile("src.txt", "Hello, World!");
		string dst = CreateFile("dst.txt", "Goodbye, World!");
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		bool result = await service.CompareAsync(src, dst, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	[Fact]
	public async Task CompareAsync_SameFile_ReturnsTrue()
	{
		string path = CreateFile("same.txt", "content");
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		bool result = await service.CompareAsync(path, path, TestContext.Current.CancellationToken);

		Assert.True(result);
	}

	[Fact]
	public async Task CompareAsync_ZeroLengthFiles_ReturnsTrue()
	{
		string src = CreateFile("empty1.txt", "");
		string dst = CreateFile("empty2.txt", "");
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		bool result = await service.CompareAsync(src, dst, TestContext.Current.CancellationToken);

		Assert.True(result);
	}

	[Fact]
	public async Task CompareAsync_SameSizeDifferentContent_ReturnsFalse()
	{
		string src = CreateFile("src.bin", new byte[] { 0x01, 0x02, 0x03, 0x04 });
		string dst = CreateFile("dst.bin", new byte[] { 0x01, 0x02, 0xFF, 0x04 });
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		bool result = await service.CompareAsync(src, dst, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	[Fact]
	public async Task CompareAsync_DifferentSizes_ReturnsFalse()
	{
		string src = CreateFile("src.txt", "short");
		string dst = CreateFile("dst.txt", "longer content");
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		bool result = await service.CompareAsync(src, dst, TestContext.Current.CancellationToken);

		Assert.False(result);
	}

	[Fact]
	public async Task CompareAsync_SourceNotExists_ThrowsArgumentException()
	{
		string dst = CreateFile("dst.txt", "content");
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		await Assert.ThrowsAsync<ArgumentException>(() =>
			service.CompareAsync("Z:\\nonexistent\\src.txt", dst, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task CompareAsync_BothNotExists_ThrowsArgumentException()
	{
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		await Assert.ThrowsAsync<ArgumentException>(() =>
			service.CompareAsync("Z:\\nonexistent\\a.txt", "Z:\\nonexistent\\b.txt", TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task CompareAsync_LargeFiles_ComparesCorrectly()
	{
		byte[] content = new byte[1_000_000];
		new Random(42).NextBytes(content);
		string src = CreateFile("large_src.bin", content);
		string dst = CreateFile("large_dst.bin", content);
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		bool result = await service.CompareAsync(src, dst, TestContext.Current.CancellationToken);

		Assert.True(result);
	}

	[Fact]
	public async Task CompareAsync_Cancelled_Throws()
	{
		string src = CreateFile("src_cancel.txt", "content");
		string dst = CreateFile("dst_cancel.txt", "content");
		FileCompareService service = new(new BinaryFileComparerSelector(
			new WholeFileSequenceEqualBinaryComparer(),
			new ChunkedSequenceEqualBinaryComparer(),
			new ChunkedVectorBinaryComparer(),
			new ChunkedEightByteBinaryComparer(),
			new ChunkedAvx2BinaryComparer()));

		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<TaskCanceledException>(() =>
			service.CompareAsync(src, dst, cts.Token));
	}

	private string CreateFile(string name, string content)
	{
		string path = Path.Combine(_tempDir, name);
		File.WriteAllText(path, content);
		return path;
	}

	private string CreateFile(string name, byte[] content)
	{
		string path = Path.Combine(_tempDir, name);
		File.WriteAllBytes(path, content);
		return path;
	}

	public void Dispose()
	{
		if(Directory.Exists(_tempDir))
			try { Directory.Delete(_tempDir, recursive: true); } catch { }
	}
}
