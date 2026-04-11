using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;

namespace BMTP3.Core2.BackupNew.Engine.Orchestration;

/// <summary>
///     A relaxed JobValidator intended for tests and CI. It performs only minimal validation
///     (presence of SourceType/SourceId/SourcePath and valid enums) and does not attempt to
///     create output directories or check free disk space.
///     Register in tests via services.AddBMTP3Core2(s => s.AddSingleton<IJobValidator, RelaxedJobValidator>());
/// </summary>
public class RelaxedJobValidator : IJobValidator
{
	public Task ValidateAsync(BackupPlan plan, CancellationToken ct)
	{
		if (plan == null)
		{
			throw new ArgumentNullException(nameof(plan));
		}

		if (!Enum.IsDefined(typeof(SourceType), plan.SourceType))
		{
			throw new ArgumentException($"Invalid SourceType: {plan.SourceType}");
		}

		if (string.IsNullOrWhiteSpace(plan.SourceId))
		{
			throw new ArgumentException("SourceId is required (device, drive or root path).");
		}

		if (string.IsNullOrWhiteSpace(plan.SourcePath))
		{
			throw new ArgumentException("SourcePath is required (subfolder or backing path).");
		}

		// Skip OutputPath creation and free-space checks to avoid platform-specific failures in CI
		return Task.CompletedTask;
	}
}