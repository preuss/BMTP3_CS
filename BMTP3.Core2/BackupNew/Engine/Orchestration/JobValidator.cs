using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;

namespace BMTP3.Core2.BackupNew.Engine.Orchestration;

public interface IJobValidator
{
	Task ValidateAsync(BackupPlan plan, CancellationToken ct);
}

public class JobValidator : IJobValidator
{
	public Task ValidateAsync(BackupPlan plan, CancellationToken ct)
	{
		ValidateDomainRules(plan);
		return Task.CompletedTask;
	}

	private static void ValidateDomainRules(BackupPlan plan)
	{
		List<string> errors = new();

		if(string.IsNullOrWhiteSpace(plan.Name))
			errors.Add("Name is required.");

		if(string.IsNullOrWhiteSpace(plan.OutputPath))
			errors.Add("OutputPath is required.");

		if(string.IsNullOrWhiteSpace(plan.SourceId))
			errors.Add("SourceId is required.");

		if(string.IsNullOrWhiteSpace(plan.SourcePath))
			errors.Add("SourcePath is required.");

		if(!Enum.IsDefined(typeof(SourceType), plan.SourceType))
			errors.Add($"Invalid SourceType value: {(int)plan.SourceType}.");

		if(!Enum.IsDefined(typeof(PostWriteVerificationType), plan.PostWriteVerification))
			errors.Add($"Invalid PostWriteVerification value: {(int)plan.PostWriteVerification}.");

		if(!Enum.IsDefined(typeof(SidecarFormat), plan.SidecarFormat))
			errors.Add($"Invalid SidecarFormat value: {(int)plan.SidecarFormat}.");

		if(!Enum.IsDefined(typeof(BackupIndexType), plan.BackupIndexType))
			errors.Add($"Invalid BackupIndexType value: {(int)plan.BackupIndexType}.");

		if(!Enum.IsDefined(typeof(CollisionComparisonType), plan.ComparisonType))
			errors.Add($"Invalid CollisionComparisonType value: {(int)plan.ComparisonType}.");

		if(!Enum.IsDefined(typeof(CollisionResolutionType), plan.CollisionResolution))
			errors.Add($"Invalid CollisionResolutionType value: {(int)plan.CollisionResolution}.");

		if(!Enum.IsDefined(typeof(RenameStrategy), plan.RenameStrategy))
			errors.Add($"Invalid RenameStrategy value: {(int)plan.RenameStrategy}.");

		if(plan.OutputStrategy == OutputStructureStrategy.CustomPathPattern && string.IsNullOrWhiteSpace(plan.CustomOutputPathPattern))
			errors.Add("CustomOutputPathPattern is required when OutputStrategy is CustomPathPattern.");

		if(plan.RenameStrategy == RenameStrategy.CustomCollisionPathPattern && string.IsNullOrWhiteSpace(plan.CustomCollisionPathPattern))
			errors.Add("CustomCollisionPathPattern is required when RenameStrategy is CustomCollisionPathPattern.");

		if(plan.HashTypes == null || plan.HashTypes.Count == 0)
			errors.Add("HashTypes must contain at least one hash algorithm.");

		if(plan.VerificationRetryCount < 1)
			errors.Add($"VerificationRetryCount must be >= 1, got {plan.VerificationRetryCount}.");

		if(plan.VerificationRetryDelayMs < 0)
			errors.Add($"VerificationRetryDelayMs must be >= 0, got {plan.VerificationRetryDelayMs}.");

		if(plan.VerificationTimeoutMs < 0)
			errors.Add($"VerificationTimeoutMs must be >= 0, got {plan.VerificationTimeoutMs}.");

		if(plan.DelayMs < 0)
			errors.Add($"DelayMs must be >= 0, got {plan.DelayMs}.");

		if(errors.Count > 0)
			throw new BackupPlanValidationException(errors);
	}
}