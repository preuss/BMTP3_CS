using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Session;
internal interface ISessionStateService
{
	Task ApplyResumeAsync(
		IReadOnlyList<BackupRecord> records,
		BackupSessionKey sessionKey,
		SessionResumeStrategy resumeBehavior,
		CancellationToken ct);
	Task SaveAsync(
		IReadOnlyList<BackupRecord> records,
		BackupSessionKey sessionKey,
		CancellationToken ct);
	Task DeleteAsync(CancellationToken ct);
}