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

		if(plan.SourceType is null)
			throw new BackupPlanArgumentException("SourceType must be specified. Use --source-type or set 'source.type' in config.");

		if(!Enum.IsDefined(plan.SourceType.Value))
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

		if(plan.CollisionComparisonType == CollisionComparisonType.Hash && plan.ComparisonHashAlgorithmTypes is not { Count: > 0 })
			throw new BackupPlanArgumentException("ComparisonHashAlgorithmTypes must not be null and must contain at least one algorithm when CollisionComparisonType is Hash.");

		if(plan.PostWriteVerification == PostWriteVerificationType.Hash && plan.VerificationHashAlgorithmTypes is not { Count: > 0 })
			throw new BackupPlanArgumentException("VerificationHashAlgorithmTypes must not be null and must contain at least one algorithm when PostWriteVerification is Hash.");

		if(!Enum.IsDefined(plan.PostWriteVerification))
			throw new BackupPlanArgumentException($"Invalid PostWriteVerification value: {plan.PostWriteVerification}.");

		if(!Enum.IsDefined(plan.ResumeBehavior))
			throw new BackupPlanArgumentException($"Invalid ResumeBehavior value: {plan.ResumeBehavior}.");

		// Delay is always valid (-1 = disabled, 0 = 0ms, >0 = N ms).
		// No validation needed — any int is acceptable.

		// --- Glob patterns must use '/' separator ---
		//
		// GlobMatcher uses '/' as its path separator. Backslash ('\') is
		// treated as a literal character, not a separator. Patterns
		// containing '\' will never match any file path.
		//
		// Users coming from Windows conventions may write patterns like
		// "DCIM\100APPLE\*.jpg" out of habit. Reject early with a clear
		// message.

		if (plan.IncludePatterns is { Count: > 0 })
		{
			foreach (string pattern in plan.IncludePatterns)
			{
				if (pattern.Contains('\\'))
					throw new BackupPlanArgumentException(
						$"Include pattern '{pattern}' must use '/', not '\\'.");
			}
		}

		if (plan.ExcludePatterns is { Count: > 0 })
		{
			foreach (string pattern in plan.ExcludePatterns)
			{
				if (pattern.Contains('\\'))
					throw new BackupPlanArgumentException(
						$"Exclude pattern '{pattern}' must use '/', not '\\'.");
			}
		}

		// --- Tier-gating (detect genuinely missing features) ---

		// Tier 4 — Features not implemented at all
		if(plan.BackupIndexType == BackupIndexType.Database)
			throw new FeatureNotImplementedException(4, "Backup index: Database");
	}
}
