using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Api.Exceptions;

/// <summary>
/// Thrown when a <see cref="BackupPlan"/> contains invalid or unsupported
/// configuration and the backup job cannot be started.
/// </summary>
public class BackupPlanValidationException : Exception
{
    public BackupPlanValidationException(string message) : base(message)
    {
    }
}
