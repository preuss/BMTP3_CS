using Microsoft.Extensions.Logging;
using BMTP3.Core3.Hashing;
using BMTP3.Core3.Scanning;
using BMTP3.Core3.Transfer;
using BMTP3.Core3.Metadata;
using BMTP3.Core3.Sidecar;
using Xunit;

namespace BMTP3.Core3.Tests.Engine;

/// <summary>
/// Integration tests for BackupEngineSequential with real filesystem operations.
/// Verifies complete backup pipeline works end-to-end.
/// </summary>
[Trait("Category", "Integration")]
public class BackupEngineIntegrationTests : IDisposable
{
	private readonly string _testSourceDir;
	private readonly string _testDestDir;
	private readonly ILoggerFactory _loggerFactory;

	public BackupEngineIntegrationTests()
	{
		_testSourceDir = Path.Combine(Path.GetTempPath(), $"bmtp3-test-src-{Guid.NewGuid():N}");
		_testDestDir = Path.Combine(Path.GetTempPath(), $"bmtp3-test-dst-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_testSourceDir);
		Directory.CreateDirectory(_testDestDir);

		_loggerFactory = new LoggerFactory();
	}

	public void Dispose()
	{
		try { Directory.Delete(_testSourceDir, recursive: true); } catch { }
		try { Directory.Delete(_testDestDir, recursive: true); } catch { }
		_loggerFactory?.Dispose();
	}

	private BackupEngineSequential CreateEngine()
	{
		var scanner = new FileSystemScanner(_loggerFactory.CreateLogger<FileSystemScanner>());
		var transfer = new SimpleFileTransfer(_loggerFactory.CreateLogger<SimpleFileTransfer>());
		var streamHasher = new StreamHashGenerator(_loggerFactory.CreateLogger<StreamHashGenerator>());
		var hasher = new ItemHasher(_loggerFactory.CreateLogger<ItemHasher>(), streamHasher);
		var metadata = new FileMetadataReader(_loggerFactory.CreateLogger<FileMetadataReader>());
		var sidecar = new SimpleSidecarGenerator(_loggerFactory.CreateLogger<SimpleSidecarGenerator>());
		return new BackupEngineSequential(scanner, transfer, hasher, metadata, sidecar, _loggerFactory.CreateLogger<BackupEngineSequential>());
	}

	[Fact]
	public async Task RunAsync_WithSingleFile_CompleteSuccessfully()
	{
		// Arrange: Create a single test file
		var testFile = Path.Combine(_testSourceDir, "test.txt");
		var testContent = "Hello, World! This is a backup test.";
		await File.WriteAllTextAsync(testFile, testContent);
		File.SetLastWriteTime(testFile, new DateTime(2024, 1, 15, 10, 30, 0));

		var engine = CreateEngine();
		var plan = new BackupPlan
		{
			Source = _testSourceDir,
			Destination = _testDestDir,
			HashTypes = new() { HashType.SHA2_256, HashType.MD5_128 }
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.True(result.Success, $"Backup failed: {string.Join(", ", result.Errors)}");
		Assert.Equal(1, result.TotalItems);
		Assert.Equal(0, result.FailedItems);

		// Verify file was copied
		var copiedFile = Path.Combine(_testDestDir, "test.txt");
		Assert.True(File.Exists(copiedFile), "File not copied");
		var copiedContent = await File.ReadAllTextAsync(copiedFile);
		Assert.Equal(testContent, copiedContent);

		// Verify sidecar exists
		var sidecar = Path.Combine(_testDestDir, "test.txt.sidecar");
		Assert.True(File.Exists(sidecar), "Sidecar not created");
	}

	[Fact]
	public async Task RunAsync_WithAllHashTypes_ComputesAllHashes()
	{
		// Arrange
		var testFile = Path.Combine(_testSourceDir, "test.bin");
		await File.WriteAllBytesAsync(testFile, new byte[] { 1, 2, 3, 4, 5 });

		var engine = CreateEngine();
		var plan = new BackupPlan
		{
			Source = _testSourceDir,
			Destination = _testDestDir,
			HashTypes = new()
			{
				HashType.SHA2_256,
				HashType.SHA2_512,
				HashType.MD5_128,
				HashType.SHA3_256_FIPS202,
				HashType.SHA3_512_FIPS202,
				HashType.SHA3_256_KECCAK,
				HashType.SHA3_512_KECCAK,
				HashType.BLAKE3_256,
				HashType.BLAKE3_512
			}
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.True(result.Success, $"Backup failed: {string.Join(", ", result.Errors)}");
		var item = result.Items.First();
		Assert.NotNull(item.Hashes);
		Assert.Equal(9, item.Hashes.Count);
	}

	[Fact]
	public async Task RunAsync_WithEmptyDirectory_Succeeds()
	{
		var engine = CreateEngine();
		var plan = new BackupPlan
		{
			Source = _testSourceDir,
			Destination = _testDestDir,
			HashTypes = new() { HashType.SHA2_256 }
		};

		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		Assert.True(result.Success);
		Assert.Equal(0, result.TotalItems);
	}

	[Fact]
	public async Task RunAsync_WithMultipleFiles_ProcessesAll()
	{
		// Arrange: Create 3 files
		for (int i = 1; i <= 3; i++)
		{
			var file = Path.Combine(_testSourceDir, $"file{i}.txt");
			await File.WriteAllTextAsync(file, $"Content {i}");
		}

		var engine = CreateEngine();
		var plan = new BackupPlan
		{
			Source = _testSourceDir,
			Destination = _testDestDir,
			HashTypes = new() { HashType.SHA2_256 }
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.True(result.Success);
		Assert.Equal(3, result.TotalItems);
		// Verify 3 files + 3 sidecars were created
		var allFiles = Directory.GetFiles(_testDestDir);
		Assert.Equal(6, allFiles.Length);
	}
}
