using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.BackupNew.Engine.Orchestration;

public interface IJobValidator
{
	Task ValidateAsync(BackupPlan plan, CancellationToken ct);
}

public class JobValidator : IJobValidator
{
	public Task ValidateAsync(BackupPlan plan, CancellationToken ct)
	{
		// Domain rules are owned by BackupPlan itself — delegate first.
		plan.Validate();

		// IO-level checks that require runtime environment (disk space, directory access).
		// These cannot live in BackupPlan because BackupPlan is a pure domain object.
		DirectoryInfo outputDir = new(plan.OutputPath);
		if(!outputDir.Exists)
		{
			// Try create to ensure access and catch potential issues early (e.g. permissions).
			outputDir.Create();
		}

		// Check space (heuristic: warn if < 1GB, fail if < 10MB?)
		// Since we don't know total backup size yet (scanning hasn't happened),
		// we can only ensure we aren't completely full.
		long freeSpace = GetFreeSpace(outputDir.FullName);
		// 50 MB safety buffer
		if(freeSpace < 50 * 1024 * 1024)
		{
			throw new IOException($"Insufficient disk space on output drive '{outputDir.Root}'. Available: {freeSpace / 1024 / 1024} MB.");
		}

		return Task.CompletedTask;
	}

	private static long GetFreeSpace(string path)
	{
		try
		{
			string root = Path.GetPathRoot(path) ?? path;
			DriveInfo drive = new(root);
			return drive.AvailableFreeSpace;
		} catch
		{
			return long.MaxValue;
		}
	}
}
