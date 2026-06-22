using BMTP3.Core4.Api.Exceptions;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Validation;

namespace BMTP3.Core4.Tests.Engine.Validation;

public class BackupPlanValidatorTests
{
	private static BackupPlan ValidPlan => new()
	{
		Name = "Test Backup",
		SourcePath = @"C:\Users\Test\Pictures",
		Destination = @"D:\Backups",
	};

	// -----------------------------------------------------------------------
	// Name
	// -----------------------------------------------------------------------

	[Fact]
	public void Validate_NullName_Throws()
	{
		BackupPlan plan = ValidPlan with { Name = null! };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Name", ex.Message);
	}

	[Fact]
	public void Validate_EmptyName_Throws()
	{
		BackupPlan plan = ValidPlan with { Name = "" };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Name", ex.Message);
	}

	[Fact]
	public void Validate_WhitespaceName_Throws()
	{
		BackupPlan plan = ValidPlan with { Name = "   " };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Name", ex.Message);
	}

	// -----------------------------------------------------------------------
	// SourcePath
	// -----------------------------------------------------------------------

	[Fact]
	public void Validate_NullSourcePath_Throws()
	{
		BackupPlan plan = ValidPlan with { SourcePath = null! };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("SourcePath", ex.Message);
	}

	[Fact]
	public void Validate_EmptySourcePath_Throws()
	{
		BackupPlan plan = ValidPlan with { SourcePath = "" };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("SourcePath", ex.Message);
	}

	[Fact]
	public void Validate_WhitespaceSourcePath_Throws()
	{
		BackupPlan plan = ValidPlan with { SourcePath = "   " };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("SourcePath", ex.Message);
	}

	[Fact]
	public void Validate_InvalidSourcePathChars_Throws()
	{
		BackupPlan plan = ValidPlan with { SourcePath = "C:\test\fi|le" };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("SourcePath", ex.Message);
	}

	// -----------------------------------------------------------------------
	// Destination
	// -----------------------------------------------------------------------

	[Fact]
	public void Validate_NullDestination_Throws()
	{
		BackupPlan plan = ValidPlan with { Destination = null! };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Destination", ex.Message);
	}

	[Fact]
	public void Validate_EmptyDestination_Throws()
	{
		BackupPlan plan = ValidPlan with { Destination = "" };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Destination", ex.Message);
	}

	[Fact]
	public void Validate_WhitespaceDestination_Throws()
	{
		BackupPlan plan = ValidPlan with { Destination = "   " };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Destination", ex.Message);
	}

	[Fact]
	public void Validate_InvalidDestinationChars_Throws()
	{
		BackupPlan plan = ValidPlan with { Destination = "D:\bak\fi|le" };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Destination", ex.Message);
	}

	// -----------------------------------------------------------------------
	// Enum values — undefined values throw
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData((BackupSourceType)99)]
	public void Validate_InvalidSourceType_Throws(BackupSourceType value)
	{
		BackupPlan plan = ValidPlan with { SourceType = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("SourceType", ex.Message);
	}

	[Theory]
	[InlineData((OutputStructureStrategy)99)]
	public void Validate_InvalidOutputStructureStrategy_Throws(OutputStructureStrategy value)
	{
		BackupPlan plan = ValidPlan with { OutputStructureStrategy = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("OutputStructureStrategy", ex.Message);
	}

	[Theory]
	[InlineData((CollisionStrategy)99)]
	public void Validate_InvalidCollisionStrategy_Throws(CollisionStrategy value)
	{
		BackupPlan plan = ValidPlan with { CollisionStrategy = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("CollisionStrategy", ex.Message);
	}

	[Theory]
	[InlineData((CollisionComparisonType)99)]
	public void Validate_InvalidCollisionComparisonType_Throws(CollisionComparisonType value)
	{
		BackupPlan plan = ValidPlan with { CollisionComparisonType = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("CollisionComparisonType", ex.Message);
	}

	[Theory]
	[InlineData((RenameStrategy)99)]
	public void Validate_InvalidRenameStrategy_Throws(RenameStrategy value)
	{
		BackupPlan plan = ValidPlan with { RenameStrategy = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("RenameStrategy", ex.Message);
	}

	[Theory]
	[InlineData((SidecarFormat)99)]
	public void Validate_InvalidSidecarFormat_Throws(SidecarFormat value)
	{
		BackupPlan plan = ValidPlan with { SidecarFormat = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("SidecarFormat", ex.Message);
	}

	[Theory]
	[InlineData((PostWriteVerificationType)99)]
	public void Validate_InvalidPostWriteVerification_Throws(PostWriteVerificationType value)
	{
		BackupPlan plan = ValidPlan with { PostWriteVerification = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("PostWriteVerification", ex.Message);
	}

	[Theory]
	[InlineData((BackupIndexType)99)]
	public void Validate_InvalidBackupIndexType_Throws(BackupIndexType value)
	{
		BackupPlan plan = ValidPlan with { BackupIndexType = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("BackupIndexType", ex.Message);
	}

	[Theory]
	[InlineData((SessionResumeStrategy)99)]
	public void Validate_InvalidResumeBehavior_Throws(SessionResumeStrategy value)
	{
		BackupPlan plan = ValidPlan with { ResumeBehavior = value };

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("ResumeBehavior", ex.Message);
	}

	// -----------------------------------------------------------------------
	// Hash validation — must have algorithms when comparison/verification is Hash
	// -----------------------------------------------------------------------

	[Fact]
	public void Validate_CollisionHashWithoutAlgorithms_Throws()
	{
		BackupPlan plan = ValidPlan with
		{
			CollisionComparisonType = CollisionComparisonType.Hash,
			ComparisonHashAlgorithmTypes = null,
		};

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("ComparisonHashAlgorithmTypes", ex.Message);
	}

	[Fact]
	public void Validate_CollisionHashWithEmptyAlgorithms_Throws()
	{
		BackupPlan plan = ValidPlan with
		{
			CollisionComparisonType = CollisionComparisonType.Hash,
			ComparisonHashAlgorithmTypes = Array.Empty<HashAlgorithmType>(),
		};

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("ComparisonHashAlgorithmTypes", ex.Message);
	}

	[Fact]
	public void Validate_CollisionHashWithAlgorithms_Succeeds()
	{
		BackupPlan plan = ValidPlan with
		{
			CollisionComparisonType = CollisionComparisonType.Hash,
			ComparisonHashAlgorithmTypes = new[] { HashAlgorithmType.SHA2_256 },
		};

		BackupPlanValidator.Validate(plan);
	}

	[Fact]
	public void Validate_VerificationHashWithoutAlgorithms_Throws()
	{
		BackupPlan plan = ValidPlan with
		{
			PostWriteVerification = PostWriteVerificationType.Hash,
			VerificationHashAlgorithmTypes = null,
		};

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("VerificationHashAlgorithmTypes", ex.Message);
	}

	[Fact]
	public void Validate_VerificationHashWithEmptyAlgorithms_Throws()
	{
		BackupPlan plan = ValidPlan with
		{
			PostWriteVerification = PostWriteVerificationType.Hash,
			VerificationHashAlgorithmTypes = Array.Empty<HashAlgorithmType>(),
		};

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("VerificationHashAlgorithmTypes", ex.Message);
	}

	[Fact]
	public void Validate_VerificationHashWithAlgorithms_Succeeds()
	{
		BackupPlan plan = ValidPlan with
		{
			PostWriteVerification = PostWriteVerificationType.Hash,
			VerificationHashAlgorithmTypes = new[] { HashAlgorithmType.SHA2_256 },
		};

		BackupPlanValidator.Validate(plan);
	}

	// -----------------------------------------------------------------------
	// Glob patterns — backslash rejected
	// -----------------------------------------------------------------------

	[Fact]
	public void Validate_IncludePatternWithBackslash_Throws()
	{
		BackupPlan plan = ValidPlan with
		{
			IncludePatterns = new[] { "DCIM\\100APPLE\\*.jpg" },
		};

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Include pattern", ex.Message);
	}

	[Fact]
	public void Validate_ExcludePatternWithBackslash_Throws()
	{
		BackupPlan plan = ValidPlan with
		{
			ExcludePatterns = new[] { "DCIM\\100APPLE\\*.jpg" },
		};

		BackupPlanArgumentException ex = Assert.Throws<BackupPlanArgumentException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Contains("Exclude pattern", ex.Message);
	}

	[Fact]
	public void Validate_ForwardSlashPatterns_Succeeds()
	{
		BackupPlan plan = ValidPlan with
		{
			IncludePatterns = new[] { "DCIM/100APPLE/*.jpg" },
			ExcludePatterns = new[] { "**/*.tmp" },
		};

		BackupPlanValidator.Validate(plan);
	}

	// -----------------------------------------------------------------------
	// Feature gate — BackupIndexType.Database
	// -----------------------------------------------------------------------

	[Fact]
	public void Validate_DatabaseBackupIndex_ThrowsFeatureNotImplemented()
	{
		BackupPlan plan = ValidPlan with { BackupIndexType = BackupIndexType.Database };

		FeatureNotImplementedException ex = Assert.Throws<FeatureNotImplementedException>(
			() => BackupPlanValidator.Validate(plan));
		Assert.Equal(4, ex.FeatureTier);
		Assert.Contains("Database", ex.FeatureName);
	}

	// -----------------------------------------------------------------------
	// Happy path
	// -----------------------------------------------------------------------

	[Fact]
	public void Validate_ValidMinimalPlan_Succeeds()
	{
		BackupPlanValidator.Validate(ValidPlan);
	}

	[Fact]
	public void Validate_ValidFullPlan_Succeeds()
	{
		BackupPlan plan = ValidPlan with
		{
			SourceType = BackupSourceType.FileSystem,
			Recursive = true,
			IncludePatterns = new[] { "**/*.jpg", "**/*.png" },
			ExcludePatterns = new[] { "**/*.tmp" },
			OutputStructureStrategy = OutputStructureStrategy.Flat,
			CollisionStrategy = CollisionStrategy.Skip,
			CollisionComparisonType = CollisionComparisonType.Binary,
			RenameStrategy = RenameStrategy.Timestamp,
			SidecarFormat = SidecarFormat.Json,
			PostWriteVerification = PostWriteVerificationType.None,
			ResumeBehavior = SessionResumeStrategy.Continue,
			Delay = 100,
			DryRun = true,
			StopOnError = true,
			EnableTimestampCorrection = true,
		};

		BackupPlanValidator.Validate(plan);
	}
}
