using System.Collections.ObjectModel;

namespace BMTP3.Core2.BackupNew.Api.Request;

/// <summary>
///     Thrown when a <see cref="BackupPlan" /> fails domain-level validation.
/// </summary>
public sealed class BackupPlanValidationException : Exception
{
	public BackupPlanValidationException(IList<string> errors)
		: base($"BackupPlan validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"))}")
	{
		Errors = new ReadOnlyCollection<string>(errors);
	}

	/// <summary>
	///     All validation errors collected during <see cref="BackupPlan.Validate" />.
	/// </summary>
	public ReadOnlyCollection<string> Errors { get; }
}