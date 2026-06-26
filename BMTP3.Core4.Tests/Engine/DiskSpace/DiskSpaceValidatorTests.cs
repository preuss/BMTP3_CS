using BMTP3.Core4.Engine.DiskSpace;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core4.Tests.Engine.DiskSpace;

public class DiskSpaceValidatorTests
{
	private readonly DiskSpaceValidator _validator = new(NullLogger<DiskSpaceValidator>.Instance);

	[Fact]
	public async Task EnsureMinimumFreeSpaceAsync_NullPath_Throws()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			_validator.EnsureMinimumFreeSpaceAsync(null!, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task EnsureMinimumFreeSpaceAsync_CurrentDrive_DoesNotThrow()
	{
		await _validator.EnsureMinimumFreeSpaceAsync(Directory.GetCurrentDirectory(), TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task EnsureMinimumFreeSpaceAsync_CancelledToken_Throws()
	{
		using CancellationTokenSource cts = new();
		cts.Cancel();
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			_validator.EnsureMinimumFreeSpaceAsync(Directory.GetCurrentDirectory(), cts.Token));
	}

	[Fact]
	public async Task EnsureSufficientBackupCapacityAsync_ZeroBytes_DoesNothing()
	{
		await _validator.EnsureSufficientBackupCapacityAsync(Directory.GetCurrentDirectory(), 0, TestContext.Current.CancellationToken);
		await _validator.EnsureSufficientBackupCapacityAsync(Directory.GetCurrentDirectory(), -1, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task EnsureSufficientBackupCapacityAsync_NullPath_Throws()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			_validator.EnsureSufficientBackupCapacityAsync(null!, 100, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task EnsureSufficientBackupCapacityAsync_CancelledToken_Throws()
	{
		using CancellationTokenSource cts = new();
		cts.Cancel();
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			_validator.EnsureSufficientBackupCapacityAsync(Directory.GetCurrentDirectory(), 100, cts.Token));
	}

	[Fact]
	public async Task EnsureSufficientBackupCapacityAsync_SmallAmount_DoesNotThrow()
	{
		await _validator.EnsureSufficientBackupCapacityAsync(Directory.GetCurrentDirectory(), 1, TestContext.Current.CancellationToken);
	}
}
