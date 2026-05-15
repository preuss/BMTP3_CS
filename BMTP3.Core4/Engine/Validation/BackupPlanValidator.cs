using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Engine.Validation;

internal static class BackupPlanValidator
{
	public static void Validate(BackupPlan plan)
	{
		List<string> errors = new();

		if(string.IsNullOrWhiteSpace(plan.Name))
			errors.Add("Name is required.");

		if(string.IsNullOrWhiteSpace(plan.Source))
			errors.Add("Source is required.");

		if(string.IsNullOrWhiteSpace(plan.Destination))
			errors.Add("Destination is required.");

		if(!Enum.IsDefined(plan.SourceType))
			errors.Add($"Invalid SourceType value: {plan.SourceType}.");

		if(!Enum.IsDefined(plan.OutputStructure))
			errors.Add($"Invalid OutputStructure value: {plan.OutputStructure}.");

		if(!Enum.IsDefined(plan.CollisionStrategy))
			errors.Add($"Invalid CollisionStrategy value: {plan.CollisionStrategy}.");

		if(plan.MaxDegreeOfParallelism.HasValue &&
			plan.MaxDegreeOfParallelism.Value <= 0)
		{
			errors.Add("MaxDegreeOfParallelism must be greater than zero.");
		}

		if(errors.Count > 0)
			throw new BackupPlanValidationException(errors);
	}
}
