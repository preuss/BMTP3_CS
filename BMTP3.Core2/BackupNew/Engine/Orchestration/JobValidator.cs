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
		// 1. Validate Output Path Access and Space
		if(string.IsNullOrWhiteSpace(plan.OutputPath))
		{
			throw new ArgumentException("Output path is required.");
		}

		DirectoryInfo outputDir = new DirectoryInfo(plan.OutputPath);
		if(!outputDir.Exists)
		{
			// Try create to ensure access
			outputDir.Create();
		}

		// Check space (heuristic: warn if < 1GB, fail if < 10MB?)
		// Since we don't know total backup size yet (scanning hasn't happened),
		// we can only ensure we aren't completely full.
		long freeSpace = GetFreeSpace(outputDir.FullName);
		if(freeSpace < 50 * 1024 * 1024) // 50 MB safety buffer
		{
			throw new IOException($"Insufficient disk space on output drive '{outputDir.Root}'. Available: {freeSpace / 1024 / 1024} MB.");
		}

		// 2. Validate Source Type, SourceId and SourcePath
		if(!Enum.IsDefined(typeof(SourceType), plan.SourceType))
		{
			throw new ArgumentException($"Invalid SourceType: {plan.SourceType}");
		}

		if(string.IsNullOrWhiteSpace(plan.SourceId))
		{
			throw new ArgumentException("SourceId is required (device, drive or root path).");
		}

		if(string.IsNullOrWhiteSpace(plan.SourcePath))
		{
			throw new ArgumentException("SourcePath is required (subfolder or backing path).");
		}

		// 3. Validate SidecarFormat
		if(!Enum.IsDefined(typeof(SidecarFormat), plan.SidecarFormat))
		{
			throw new ArgumentException($"Invalid SidecarFormat: {plan.SidecarFormat}");
		}

		// 4. Validate CollisionComparisonType and CollisionResolutionType
		if(!Enum.IsDefined(typeof(CollisionComparisonType), plan.ComparisonType))
		{
			throw new ArgumentException($"Invalid CollisionComparisonType: {plan.ComparisonType}");
		}

		if(!Enum.IsDefined(typeof(CollisionResolutionType), plan.CollisionResolution))
		{
			throw new ArgumentException($"Invalid CollisionResolutionType: {plan.CollisionResolution}");
		}

		// 5. Validate HashTypes
		if(plan.HashTypes == null || plan.HashTypes.Count == 0)
		{
			throw new ArgumentException("HashTypes must include at least one hash algorithm for integrity checks.");
		}

		return Task.CompletedTask;
	}

	private long GetFreeSpace(string path)
	{
		try
		{
			string root = Path.GetPathRoot(path) ?? path;
			DriveInfo drive = new DriveInfo(root);
			return drive.AvailableFreeSpace;
		} catch
		{
			// If we can't determine space (e.g. UNC path sometimes), assume OK or return max
			return long.MaxValue;
		}
	}
}
