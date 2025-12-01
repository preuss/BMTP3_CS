namespace BMTP3.Core2.BackupNew.Models;

public class BackupActionResult
{
    public BackupActionType Action { get; init; }
    public string Reason { get; init; }

    public BackupActionResult(BackupActionType action, string reason = "")
    {
        Action = action;
        Reason = reason;
    }
}
