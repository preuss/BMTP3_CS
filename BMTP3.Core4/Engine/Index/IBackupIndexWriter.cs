using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Index;

internal interface IBackupIndexWriter
{
	Task WriteAsync(
		string destinationDirectory,
		string sessionId,
		IReadOnlyList<BackupRecord> records,
		BackupPlan plan,
		BackupResult result,
		CancellationToken cancellationToken
	);
}
