using BMTP3.Core4.Api.Models;

namespace BMTP3.Core4.Api.Exceptions;

/// <summary>
/// Thrown when a <see cref="BackupPlan"/> contains invalid or unsupported
/// configuration and the backup job cannot be started.
/// </summary>
public class BackupPlanValidationException : Exception
{
    /// <summary>
    /// The individual validation errors that caused this exception.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    public BackupPlanValidationException(string message) : base(message)
    {
        Errors = Array.Empty<string>();
    }

    public BackupPlanValidationException(IEnumerable<string> errors) : base("BackupPlan validation failed.")
    {
        Errors = errors.ToList();
    }
}
