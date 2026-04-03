namespace BMTP3.Core2.BackupNew.Api.UI;

/// <summary>
/// Abstraction for user-facing, non-interactive notifications from the library to the UI.
/// Core2 should only depend on this interface; concrete implementations live in the UI project.
/// </summary>
public interface IUserNotifier
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}
