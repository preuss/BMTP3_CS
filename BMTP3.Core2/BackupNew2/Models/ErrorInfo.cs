namespace BMTP3.Core2.BackupNew2.Models;

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
		BackupError error = new()
		{
			StageName = stepName,
			StepName = stepName,
			Message = message,
			Timestamp = timestamp,
			Exception = ex,
			ExceptionType = ex?.GetType().Name,
			StackTrace = ex?.StackTrace
		};

		lock (_errors)
		{
			_errors.Add(error);
		}
	}

	public override string ToString()
	{
		return HasErrors
			? $"{Count} error(s) – latest: {_errors.Last().Message}"
			: "No errors";
	}
}