using BMTP3.Core4.Api.Exceptions;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Engine.Validation;

internal static class BackupPlanValidator
{
	public static void Validate(BackupPlan plan)
	{
		List<string> errors = new();

		// --- Structural validation ---

		if(string.IsNullOrWhiteSpace(plan.Name)) errors.Add("Name is required.");

		if(string.IsNullOrWhiteSpace(plan.Source)) errors.Add("Source is required.");

		if(string.IsNullOrWhiteSpace(plan.Destination)) errors.Add("Destination is required.");

		if(!Enum.IsDefined(plan.SourceType)) errors.Add($"Invalid SourceType value: {plan.SourceType}.");

		if(!Enum.IsDefined(plan.OutputStructure)) errors.Add($"Invalid OutputStructure value: {plan.OutputStructure}.");

		if(!Enum.IsDefined(plan.CollisionStrategy)) errors.Add($"Invalid CollisionStrategy value: {plan.CollisionStrategy}.");

		if(plan.MaxDegreeOfParallelism.HasValue && plan.MaxDegreeOfParallelism.Value <= 0) errors.Add("MaxDegreeOfParallelism must be greater than zero.");

		if(errors.Count > 0) throw new BackupPlanValidationException(errors);

		// --- Tier-gating (detect features from higher Tiers) ---

		if(plan.SourceType == BackupSourceType.MediaDevice)
			throw new FeatureNotImplementedException(2, "MediaDevice source");

		if(plan.IncludePatterns is { Count: > 0 })
			throw new FeatureNotImplementedException(2, "Include patterns");

		if(plan.ExcludePatterns is { Count: > 0 })
			throw new FeatureNotImplementedException(2, "Exclude patterns");

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
