using System;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;

namespace BMTP3.Core2.BackupNew2.Models.Internal;

public class BackupItem : IBackupItem
{
    public ISourceContent Content { get; }
    public Metadata Metadata { get; } = new Metadata();
    public BackupState State { get; set; } = BackupState.New;
    public BackupActionType Action { get; set; } = BackupActionType.Unknown;
    public ErrorInfo? ErrorInfo { get; set; }

    public BackupItem(ISourceContent content)
    {
        Content = content;
    }

    public void Fail(string message, string stepName, Exception? ex = null)
    {
        State = BackupState.Failed;
        Action = BackupActionType.Stop; // Or Error
        ErrorInfo = new ErrorInfo
        {
            Message = message,
            StepName = stepName,
            Exception = ex,
            Timestamp = DateTime.UtcNow
        };
    }
}
