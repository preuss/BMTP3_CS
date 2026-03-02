namespace BMTP3.Core2.BackupNew.Engine.Models
{
	// Simple operation result for transfers and similar actions.
	public class OperationResult
	{
		public bool Success { get; set; }
		public string? Message { get; set; }
		public static OperationResult Ok() => new OperationResult { Success = true };
		public static OperationResult Fail(string message) => new OperationResult { Success = false, Message = message };
		public static OperationResult Skipped(string reason) => new OperationResult { Success = true, Message = "Skipped: " + reason };
	}
}