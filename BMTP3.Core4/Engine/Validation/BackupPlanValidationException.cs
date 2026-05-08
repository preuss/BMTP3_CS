namespace BMTP3.Core4.Engine.Validation;

/// <summary>
/// Thrown when a BackupPlan contains invalid configuration
/// and the backup job cannot be started.
/// </summary>
internal sealed class BackupPlanValidationException : Exception
{
	public IReadOnlyList<string> Errors { get; }

	public BackupPlanValidationException(IEnumerable<string> errors)
		: base("BackupPlan validation failed.")
	{
		Errors = errors.ToList();
	}
}