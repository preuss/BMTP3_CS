using BMTP3.Core4.Api.Exceptions;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Engine.Validation;

internal static class BackupPlanValidator
{
	public static void Validate(BackupPlan plan)
	{
		// --- Structural validation (fail-first) ---

		if(string.IsNullOrWhiteSpace(plan.Name))
			throw new BackupPlanArgumentException("Name is required.");

		if(string.IsNullOrWhiteSpace(plan.SourcePath))
			throw new BackupPlanArgumentException("SourcePath is required.");

		if(string.IsNullOrWhiteSpace(plan.Destination))
			throw new BackupPlanArgumentException("Destination is required.");

		if(!Enum.IsDefined(plan.SourceType))
			throw new BackupPlanArgumentException($"Invalid SourceType value: {plan.SourceType}.");

		if(!Enum.IsDefined(plan.OutputStructure))
			throw new BackupPlanArgumentException($"Invalid OutputStructure value: {plan.OutputStructure}.");

		if(!Enum.IsDefined(plan.CollisionStrategy))
			throw new BackupPlanArgumentException($"Invalid CollisionStrategy value: {plan.CollisionStrategy}.");

		if(!Enum.IsDefined(plan.CollisionComparisonType))
			throw new BackupPlanArgumentException($"Invalid CollisionComparisonType value: {plan.CollisionComparisonType}.");

		if(!Enum.IsDefined(plan.SidecarFormat))
			throw new BackupPlanArgumentException($"Invalid SidecarFormat value: {plan.SidecarFormat}.");

		if(plan.MaxDegreeOfParallelism.HasValue && plan.MaxDegreeOfParallelism.Value <= 0)
			throw new BackupPlanArgumentException("MaxDegreeOfParallelism must be greater than zero.");

		// --- Tier-gating (detect features from higher Tiers) ---

		if(plan.CollisionStrategy != CollisionStrategy.Error)
			throw new FeatureNotImplementedException(2, $"Collision strategy: {plan.CollisionStrategy}");

		if(plan.CollisionComparisonType == CollisionComparisonType.Hash)
			throw new FeatureNotImplementedException(3, "Collision comparison: Hash");

		if(plan.CollisionComparisonType == CollisionComparisonType.Binary)
			throw new FeatureNotImplementedException(2, "Collision comparison: Binary");

		if(plan.SidecarFormat == SidecarFormat.None)
			throw new FeatureNotImplementedException(2, "Sidecar format: None");

		if(plan.SidecarFormat == SidecarFormat.Json)
			throw new FeatureNotImplementedException(2, "Sidecar format: Json");

		if(plan.SourceType == BackupSourceType.MediaDevice)
			throw new FeatureNotImplementedException(2, "MediaDevice source");

		if(plan.IncludePatterns is { Count: > 0 })
			throw new FeatureNotImplementedException(2, "Include patterns");

		if(plan.ExcludePatterns is { Count: > 0 })
			throw new FeatureNotImplementedException(2, "Exclude patterns");

		if(plan.DryRun)
			throw new FeatureNotImplementedException(3, "Dry run");

		if(plan.EnableHashing)
			throw new FeatureNotImplementedException(3, "Hashing");

		if(plan.EnableMetadata)
			throw new FeatureNotImplementedException(3, "Metadata extraction");

		if(plan.EnableVerification)
			throw new FeatureNotImplementedException(3, "Post-transfer verification");

		if(plan.EnableTimestampCorrection)
			throw new FeatureNotImplementedException(3, "Timestamp correction");

		if(plan.MaxDegreeOfParallelism.HasValue)
			throw new FeatureNotImplementedException(4, "Parallel execution");
	}
}
