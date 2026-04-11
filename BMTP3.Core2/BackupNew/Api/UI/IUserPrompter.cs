namespace BMTP3.Core2.BackupNew.Api.UI;

/// <summary>
///     Abstraction for interactive prompts. Implementations live in the UI layer.
///     Library code should avoid interactive prompts where possible; use with care.
/// </summary>
public interface IUserPrompter
{
	Task<bool> ConfirmAsync(string message, CancellationToken cancellationToken = default);
}