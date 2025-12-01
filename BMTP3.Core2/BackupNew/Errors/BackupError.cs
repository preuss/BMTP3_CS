using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Errors;
public class BackupError {
	public required string StageName { get; init; }
	public required string Message { get; init; }
	public DateTime Timestamp { get; init; } = DateTime.Now;
	public string? ExceptionType { get; init; }
	public string? StackTrace { get; init; }

	public override string ToString() =>
		$"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {StageName}: {Message}";
}
