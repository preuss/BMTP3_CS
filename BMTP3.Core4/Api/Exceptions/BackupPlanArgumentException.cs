using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Api.Exceptions;

/// <summary>
/// Thrown when a specific argument or property of a <see cref="BackupPlan"/>
/// contains an invalid value or fails domain-level validation.
/// This exception may be extended with argument meta-data and an errors list
/// in a future Tier.
/// </summary>
public class BackupPlanArgumentException : BackupPlanValidationException
{
	public BackupPlanArgumentException(string message) : base(message)
	{
	}
}
