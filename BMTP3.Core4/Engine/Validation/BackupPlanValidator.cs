using BMTP3.Core4.Api.Exceptions;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Engine.Validation;

internal static class BackupPlanValidator
{
	public static void Validate(BackupPlan plan)
	{
		// --- Structural validation (fail-first) ---

		if(plan.Name is null)
			throw new BackupPlanArgumentException("Name must not be null.");

		if(string.IsNullOrWhiteSpace(plan.Name))
			throw new BackupPlanArgumentException("Name must not be empty or whitespace.");

		if(plan.SourcePath is null)
			throw new BackupPlanArgumentException("SourcePath must not be null.");

		if(string.IsNullOrWhiteSpace(plan.SourcePath))
			throw new BackupPlanArgumentException("SourcePath must not be empty or whitespace.");

		if(plan.SourcePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
			throw new BackupPlanArgumentException("SourcePath contains invalid path characters.");

		if(plan.Destination is null)
			throw new BackupPlanArgumentException("Destination must not be null.");

		if(string.IsNullOrWhiteSpace(plan.Destination))
			throw new BackupPlanArgumentException("Destination must not be empty or whitespace.");

		if(plan.Destination.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
			throw new BackupPlanArgumentException("Destination contains invalid path characters.");

		if(!Enum.IsDefined(plan.SourceType))
			throw new BackupPlanArgumentException($"Invalid SourceType value: {plan.SourceType}.");

		if(!Enum.IsDefined(plan.OutputStructureStrategy))
			throw new BackupPlanArgumentException($"Invalid OutputStructureStrategy value: {plan.OutputStructureStrategy}.");

		if(!Enum.IsDefined(plan.CollisionStrategy))
			throw new BackupPlanArgumentException($"Invalid CollisionStrategy value: {plan.CollisionStrategy}.");

		if(!Enum.IsDefined(plan.CollisionComparisonType))
			throw new BackupPlanArgumentException($"Invalid CollisionComparisonType value: {plan.CollisionComparisonType}.");

		if(!Enum.IsDefined(plan.RenameStrategy))
			throw new BackupPlanArgumentException($"Invalid RenameStrategy value: {plan.RenameStrategy}.");

		if(!Enum.IsDefined(plan.SidecarFormat))
			throw new BackupPlanArgumentException($"Invalid SidecarFormat value: {plan.SidecarFormat}.");

		if(!Enum.IsDefined(plan.BackupIndexType))
			throw new BackupPlanArgumentException($"Invalid BackupIndexType value: {plan.BackupIndexType}.");

		if(plan.ComparisonHashAlgorithmTypes is not { Count: > 0 })
			throw new BackupPlanArgumentException("ComparisonHashAlgorithmTypes must not be null and must contain at least one algorithm.");

		if(plan.VerificationHashAlgorithmTypes is not { Count: > 0 })
			throw new BackupPlanArgumentException("VerificationHashAlgorithmTypes must contain at least one algorithm if specified.");

		if(!Enum.IsDefined(plan.PostWriteVerification))
			throw new BackupPlanArgumentException($"Invalid PostWriteVerification value: {plan.PostWriteVerification}.");

		if(!Enum.IsDefined(plan.ResumeBehavior))
			throw new BackupPlanArgumentException($"Invalid ResumeBehavior value: {plan.ResumeBehavior}.");

		if(plan.MaxDegreeOfParallelism.HasValue && plan.MaxDegreeOfParallelism.Value <= 0)
			throw new BackupPlanArgumentException("MaxDegreeOfParallelism must be greater than zero.");

		// --- Tier-gating (detect genuinely missing features) ---

		// Tier 3 — Features that exist but are not yet production-tested
		if(plan.EnableMetadata)
			throw new FeatureNotImplementedException(3, "Metadata extraction");

		if(plan.BackupIndexType == BackupIndexType.Json)
			throw new FeatureNotImplementedException(3, "Backup index: Json");

		// Tier 4 — Features not implemented at all
		if(plan.BackupIndexType == BackupIndexType.Database)
			throw new FeatureNotImplementedException(4, "Backup index: Database");

		if(plan.MaxDegreeOfParallelism.HasValue)
			throw new FeatureNotImplementedException(4, "Parallel execution");
	}
}
