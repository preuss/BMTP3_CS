using Microsoft.Extensions.Logging;
using BMTP3.Core3.Tests.Fakes;
using Xunit;

namespace BMTP3.Core3.Tests.Engine;

/// <summary>
/// Edge case unit tests for BackupEngineSequential.
/// Tests error conditions, boundary cases, and component interactions.
/// </summary>
public class BackupEngineSequentialEdgeCasesTests
{
	private readonly ILogger<BackupEngineSequential> _logger;

	public BackupEngineSequentialEdgeCasesTests()
	{
		var factory = new LoggerFactory();
		_logger = factory.CreateLogger<BackupEngineSequential>();
	}

	/// <summary>
	/// Verify scanner failures are captured and reported.
	/// </summary>
	[Fact]
	public async Task RunAsync_WithScannerFailure_ReturnsFailureWithError()
	{
		// Arrange
		var scanner = new FakeBackupScanner { ShouldFail = true };
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngineSequential(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan
		{
			Source = "/source",
			Destination = "/dest",
			HashTypes = new() { HashType.SHA2_256 }
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.False(result.Success);
		Assert.NotEmpty(result.Errors);
	}

	/// <summary>
	/// Verify hash computation failures are logged but sidecar generation continues.
	/// </summary>
	[Fact]
	public async Task RunAsync_WithHasherFailure_LogsWarningButContinues()
	{
		// Arrange
		var item = new TestBackupItemBuilder()
			.WithName("file.txt")
			.WithSizeInBytes(1024)
			.Build();

		var scanner = new FakeBackupScanner(item);
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher { ShouldFail = true };
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngineSequential(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan
		{
			Source = "/source",
			Destination = "/dest",
			HashTypes = new() { HashType.SHA2_256 }
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		// Hash failure should not fail the entire backup
		Assert.True(result.Success);
      Assert.NotEmpty(result.Errors);
		Assert.NotEmpty(result.Items);
	}

	/// <summary>
	/// Verify metadata extraction failures are tolerated.
	/// </summary>
	[Fact]
	public async Task RunAsync_WithMetadataReaderFailure_LogsWarningButContinues()
	{
		// Arrange
		var item = new TestBackupItemBuilder().WithName("file.txt").Build();
		var scanner = new FakeBackupScanner(item);
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader { ShouldFail = true };
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngineSequential(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan
		{
			Source = "/source",
			Destination = "/dest"
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.True(result.Success);
		Assert.NotEmpty(result.Items);
	}

	/// <summary>
	/// Verify sidecar generation failures are logged but don't fail backup.
	/// </summary>
	[Fact]
	public async Task RunAsync_WithSidecarGeneratorFailure_LogsWarningButContinues()
	{
		// Arrange
		var item = new TestBackupItemBuilder().WithName("file.txt").Build();
		var scanner = new FakeBackupScanner(item);
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator { ShouldFail = true };
		var engine = new BackupEngineSequential(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan
		{
			Source = "/source",
			Destination = "/dest"
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.True(result.Success);
        Assert.NotEmpty(result.Errors);
		Assert.Single(result.Items);
	}

	/// <summary>
	/// Verify very large number of files are all processed.
	/// </summary>
	[Fact]
	public async Task RunAsync_WithManyFiles_ProcessesAll()
	{
		// Arrange
		const int fileCount = 100;
		var items = Enumerable.Range(1, fileCount)
			.Select(i => new TestBackupItemBuilder()
				.WithName($"file{i:D4}.dat")
				.WithSizeInBytes(1024)
				.Build())
			.ToArray();

		var scanner = new FakeBackupScanner(items);
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngineSequential(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan
		{
			Source = "/source",
			Destination = "/dest"
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.True(result.Success);
		Assert.Equal(fileCount, result.TotalItems);
		Assert.Equal(fileCount, transfer.CopiedItems.Count);
		Assert.Equal(fileCount, sidecar.GeneratedSidecars.Count);
	}

	/// <summary>
	/// Verify dry run mode doesn't transfer files.
	/// </summary>
	[Fact]
	public async Task RunAsync_WithDryRunMode_DoesNotTransferFiles()
	{
		// Arrange
		var item = new TestBackupItemBuilder().WithName("file.txt").Build();
		var scanner = new FakeBackupScanner(item);
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngineSequential(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan
		{
			Source = "/source",
			Destination = "/dest",
			DryRun = true
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.True(result.Success);
		Assert.Empty(transfer.CopiedItems);
	}
}
