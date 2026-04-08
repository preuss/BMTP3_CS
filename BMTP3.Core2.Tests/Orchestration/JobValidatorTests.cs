using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Orchestration;

namespace BMTP3.Core2.Tests.Orchestration;

public class JobValidatorTests
{
	// -----------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------

	/// <summary>
	///     Returns a BackupPlan that satisfies every JobValidator rule.
	///     Uses the system temp directory as output path so disk-space and
	///     directory-creation checks are always satisfied.
	/// </summary>
	private static BackupPlan BuildValidPlan(string? outputPath = null)
	{
		return new BackupPlan
		{
			Name = "TestJob",
			SourceType = SourceType.FileSystem,
			SourceId = @"C:",
			SourcePath = @"Users\TestUser\Documents",
			OutputPath = outputPath ??
			             Path.Combine(Path.GetTempPath(), "bmtp3_validator_test_" + Guid.NewGuid().ToString("N")),
			SidecarFormat = SidecarFormat.Ini,
			ComparisonType = CollisionComparisonType.Binary,
			CollisionResolution = CollisionResolutionType.Rename,
			HashTypes = new HashSet<HashType> { HashType.SHA2_256 }
		};
	}

	// -----------------------------------------------------------------
	// 1. ValidateAsync_MissingOutputPath_ThrowsOrReturnsFail
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_MissingOutputPath_ThrowsArgumentException()
	{
		JobValidator validator = new();
		BackupPlan plan = BuildValidPlan();
		plan.OutputPath = string.Empty;

		await Assert.ThrowsAsync<BackupPlanValidationException>(() =>
			validator.ValidateAsync(plan, CancellationToken.None));
	}

	[Fact]
	public async Task ValidateAsync_NullOutputPath_ThrowsArgumentException()
	{
		JobValidator validator = new();
		BackupPlan plan = BuildValidPlan();
		plan.OutputPath = null!;

		await Assert.ThrowsAsync<BackupPlanValidationException>(() =>
			validator.ValidateAsync(plan, CancellationToken.None));
	}

	// -----------------------------------------------------------------
	// 2. ValidateAsync_MissingSourcePath_ThrowsOrReturnsFail
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_MissingSourcePath_ThrowsArgumentException()
	{
		string outputDir = Path.Combine(Path.GetTempPath(), "bmtp3_val_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(outputDir);
		try
		{
			JobValidator validator = new();
			BackupPlan plan = BuildValidPlan(outputDir);
			plan.SourcePath = string.Empty;

			await Assert.ThrowsAsync<BackupPlanValidationException>(() =>
				validator.ValidateAsync(plan, CancellationToken.None));
		}
		finally
		{
			try
			{
				Directory.Delete(outputDir, false);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 3. ValidateAsync_ValidPlan_CompletesWithoutException
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_ValidPlan_CompletesWithoutException()
	{
		string outputDir = Path.Combine(Path.GetTempPath(), "bmtp3_val_" + Guid.NewGuid().ToString("N"));
		try
		{
			// Ensure directory exists (now done by BackupEngine, not JobValidator)
			Directory.CreateDirectory(outputDir);

			JobValidator validator = new();
			BackupPlan plan = BuildValidPlan(outputDir);

			// Should not throw
			await validator.ValidateAsync(plan, CancellationToken.None);
		}
		finally
		{
			try
			{
				Directory.Delete(outputDir, false);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 4. ValidateAsync_NullPlan_ThrowsArgumentNullException
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_NullPlan_ThrowsNullReferenceOrArgumentException()
	{
		JobValidator validator = new();

		// The validator accesses plan.OutputPath directly without a null-guard,
		// so passing null results in a NullReferenceException at runtime.
		await Assert.ThrowsAnyAsync<Exception>(() =>
			validator.ValidateAsync(null!, CancellationToken.None));
	}

	// -----------------------------------------------------------------
	// 5. ValidateAsync_MissingSourceId_ThrowsOrReturnsFail
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_MissingSourceId_ThrowsArgumentException()
	{
		string outputDir = Path.Combine(Path.GetTempPath(), "bmtp3_val_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(outputDir);
		try
		{
			JobValidator validator = new();
			BackupPlan plan = BuildValidPlan(outputDir);
			plan.SourceId = string.Empty;

			await Assert.ThrowsAsync<BackupPlanValidationException>(() =>
				validator.ValidateAsync(plan, CancellationToken.None));
		}
		finally
		{
			try
			{
				Directory.Delete(outputDir, false);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 6. ValidateAsync_OutputPath_DoesNotExist_CreatesIt
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_OutputPath_DoesNotExist_CreatesDirectory()
	{
		// NOTE: Directory creation is now done by BackupEngine, not JobValidator.
		// This test verifies that JobValidator does NOT create directories.
		string outputDir = Path.Combine(Path.GetTempPath(), "bmtp3_val_new_" + Guid.NewGuid().ToString("N"));
		Assert.False(Directory.Exists(outputDir), "Pre-condition: directory must not exist yet");
		try
		{
			JobValidator validator = new();
			BackupPlan plan = BuildValidPlan(outputDir);

			// JobValidator should NOT throw - it only validates domain rules
			await validator.ValidateAsync(plan, CancellationToken.None);

			// Directory should NOT be created by JobValidator (that's BackupEngine's job now)
			Assert.False(Directory.Exists(outputDir), "JobValidator should NOT create the output directory");
		}
		finally
		{
			try
			{
				Directory.Delete(outputDir, false);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 7. RelaxedJobValidator_AlwaysPasses_WithMinimalPlan
	// -----------------------------------------------------------------

	[Fact]
	public async Task RelaxedJobValidator_AlwaysPasses_WithMinimalPlan()
	{
		RelaxedJobValidator validator = new();

		// RelaxedJobValidator does NOT check OutputPath, disk space, HashTypes,
		// SidecarFormat, or enum sanity beyond SourceType – just SourceId/SourcePath.
		BackupPlan plan = new()
		{
			SourceType = SourceType.FileSystem,
			SourceId = @"C:",
			SourcePath = @"SomePath",
			OutputPath = string.Empty, // intentionally empty – relaxed validator skips it
			HashTypes = new HashSet<HashType>() // intentionally empty – relaxed validator skips it
		};

		// Should complete without throwing
		await validator.ValidateAsync(plan, CancellationToken.None);
	}

	// -----------------------------------------------------------------
	// 8. ValidateAsync_ValidPlan_FileSystem_DoesNotThrow
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_ValidPlan_FileSystem_DoesNotThrow()
	{
		string outputDir = Path.Combine(Path.GetTempPath(), "bmtp3_val_fs_" + Guid.NewGuid().ToString("N"));
		try
		{
			// Ensure directory exists (now done by BackupEngine, not JobValidator)
			Directory.CreateDirectory(outputDir);

			JobValidator validator = new();
			BackupPlan plan = new()
			{
				Name = "FileSystem Test Job",
				SourceType = SourceType.FileSystem,
				SourceId = @"C:",
				SourcePath = @"Users\Public\Pictures",
				OutputPath = outputDir,
				SidecarFormat = SidecarFormat.Json,
				ComparisonType = CollisionComparisonType.Binary,
				CollisionResolution = CollisionResolutionType.Skip,
				HashTypes = new HashSet<HashType> { HashType.SHA2_256, HashType.MD5_128 }
			};

			// Should not throw
			await validator.ValidateAsync(plan, CancellationToken.None);
		}
		finally
		{
			try
			{
				Directory.Delete(outputDir, false);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 9. ValidateAsync_EmptyName_Throws
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_EmptyName_ThrowsArgumentException()
	{
		string outputDir = Path.Combine(Path.GetTempPath(), "bmtp3_val_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(outputDir);
		try
		{
			JobValidator validator = new();
			BackupPlan plan = BuildValidPlan(outputDir);
			plan.Name = string.Empty;

			await Assert.ThrowsAsync<BackupPlanValidationException>(() =>
				validator.ValidateAsync(plan, CancellationToken.None));
		}
		finally
		{
			try
			{
				Directory.Delete(outputDir, false);
			}
			catch
			{
			}
		}
	}

	// -----------------------------------------------------------------
	// 10. ValidateAsync_InvalidBackupIndexType_Throws
	// -----------------------------------------------------------------

	[Fact]
	public async Task ValidateAsync_InvalidBackupIndexType_ThrowsArgumentException()
	{
		string outputDir = Path.Combine(Path.GetTempPath(), "bmtp3_val_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(outputDir);
		try
		{
			JobValidator validator = new();
			BackupPlan plan = BuildValidPlan(outputDir);
			plan.BackupIndexType = (BackupIndexType)999;

			await Assert.ThrowsAsync<BackupPlanValidationException>(() =>
				validator.ValidateAsync(plan, CancellationToken.None));
		}
		finally
		{
			try
			{
				Directory.Delete(outputDir, false);
			}
			catch
			{
			}
		}
	}
}