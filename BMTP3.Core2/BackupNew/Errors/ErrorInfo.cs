using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.BackupNew.Errors;
/// <summary>
/// Holds all errors that occurred for a specific backup item.
/// The collection is read-only from the outside and thread-safe for adding errors.
/// </summary>
public class ErrorInfo {
	private readonly List<BackupError> _errors = new();

	public IReadOnlyList<BackupError> Errors => _errors.AsReadOnly();
	public bool HasErrors => _errors.Count > 0;
	public int Count => _errors.Count;

	/// <summary>
	/// Adds a new error to the collection.
	/// </summary>
	public void AddError(string stageName, string message, Exception? ex = null) {
		var error = new BackupError {
			StageName = stageName,
			Message = message,
			ExceptionType = ex?.GetType().Name,
			StackTrace = ex?.StackTrace
		};

		_errors.Add(error);
	}

	/// <summary>
	/// Adds an already constructed BackupError instance.
	/// </summary>
	public void AddError(BackupError error) {
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