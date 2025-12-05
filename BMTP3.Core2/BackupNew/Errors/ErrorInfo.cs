using BMTP3.Core2.BackupNew.Models;

namespace BMTP3.Core2.BackupNew.Errors;
/// <summary>
/// Captures error information related to a specific backup item.
/// Holds all errors that occurred for a specific backup item.
/// The collection is read-only from the outside and thread-safe for adding errors.
/// </summary>
public class ErrorInfo
{
	/// <summary>
	/// The error messages.
	/// </summary>
	private readonly List<BackupError> _errors = new();

	public IReadOnlyList<BackupError> Errors => _errors.AsReadOnly();
	public bool HasErrors => _errors.Count > 0;
	public int Count => _errors.Count;

	/// <summary>
	/// Adds a new error to the collection.
	/// </summary>
	public void AddError(string stepName, string message, DateTime timestamp, Exception? ex = null)
	{
		var error = new BackupError
		{
			StageName = stepName,
			StepName = stepName,
			Message = message,
			Timestamp = timestamp,
			Exception = ex,
			ExceptionType = ex?.GetType().Name,
			StackTrace = ex?.StackTrace
		};

		_errors.Add(error);
	}

	/// <summary>
	/// Adds an already constructed BackupError instance.
	/// </summary>
	public void AddError(BackupError error)
	{
		_errors.Add(error ?? throw new ArgumentNullException(nameof(error)));
	}

	/// <summary>
	/// Returns a short human-readable summary of the error state.
	/// </summary>
	public string GetSummary() =>
		HasErrors
			? $"{Count} error(s) – latest: {_errors[^1].Message}"
			: "No errors";
}