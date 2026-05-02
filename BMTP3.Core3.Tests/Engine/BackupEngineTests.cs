using Microsoft.Extensions.Logging;
using BMTP3.Core3.Tests.Fakes;
using Xunit;

namespace BMTP3.Core3.Tests.Engine;

/// <summary>
/// Unit tests for BackupEngine orchestrator.
/// Tests the main pipeline: Scan → Transfer → Metadata → Hashes → Timestamps → Result
/// </summary>
public class BackupEngineTests
{
	private readonly ILogger<BackupEngine> _logger;

	public BackupEngineTests()
	{
		var factory = new LoggerFactory();
		_logger = factory.CreateLogger<BackupEngine>();
	}

	[Fact]
	public async Task RunAsync_WithEmptySource_ReturnsSuccessfulResult()
	{
		// Arrange
		var scanner = new FakeBackupScanner();
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngine(scanner, transfer, hasher, metadata, sidecar, _logger);

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
		Assert.True(result.Success);
		Assert.Equal(0, result.TotalItems);
		Assert.Equal(0, result.FailedItems);
	}

	[Fact]
	public async Task RunAsync_WithSingleFile_TransfersAndHashesFile()
	{
		// Arrange
		var item = new TestBackupItemBuilder()
			.WithName("file.txt")
			.WithSizeInBytes(1024)
			.Build();

		var scanner = new FakeBackupScanner(item);
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngine(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan
		{
			Source = "/source",
			Destination = "/dest",
			HashTypes = new() { HashType.SHA2_256, HashType.MD5_128 }
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.True(result.Success);
		Assert.Single(result.Items);
		Assert.Single(transfer.CopiedItems);
		Assert.Single(sidecar.GeneratedSidecars);
	}

	[Fact]
	public async Task RunAsync_WithMultipleFiles_ProcessesAll()
	{
		// Arrange
		var items = new[]
		{
			new TestBackupItemBuilder().WithName("file1.txt").Build(),
			new TestBackupItemBuilder().WithName("file2.jpg").Build(),
			new TestBackupItemBuilder().WithName("file3.pdf").Build()
		};

		var scanner = new FakeBackupScanner(items);
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngine(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan
		{
			Source = "/source",
			Destination = "/dest"
		};

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.True(result.Success);
		Assert.Equal(3, result.TotalItems);
		Assert.Equal(3, transfer.CopiedItems.Count);
	}

	[Fact]
	public async Task RunAsync_WithCancellation_StopsProcessing()
	{
		// Arrange
		var items = new[]
		{
			new TestBackupItemBuilder().WithName("file1.txt").Build(),
			new TestBackupItemBuilder().WithName("file2.txt").Build()
		};

		var scanner = new FakeBackupScanner(items);
		var transfer = new FakeFileTransfer();
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngine(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan { Source = "/source", Destination = "/dest" };
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			engine.RunAsync(plan, null, cts.Token));
	}

	[Fact]
	public async Task RunAsync_WithTransferFailure_ReturnsFailureResult()
	{
		// Arrange
		var item = new TestBackupItemBuilder().Build();
		var scanner = new FakeBackupScanner(item);
		var transfer = new FakeFileTransfer { ShouldFail = true };
		var hasher = new FakeItemHasher();
		var metadata = new FakeMetadataReader();
		var sidecar = new FakeSidecarGenerator();
		var engine = new BackupEngine(scanner, transfer, hasher, metadata, sidecar, _logger);

		var plan = new BackupPlan { Source = "/source", Destination = "/dest" };

		// Act
		var result = await engine.RunAsync(plan, null, CancellationToken.None);

		// Assert
		Assert.NotNull(result);
		Assert.False(result.Success);
		Assert.NotEmpty(result.Errors);
	}
}
