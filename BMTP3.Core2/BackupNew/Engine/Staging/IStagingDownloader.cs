using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Api.Progress;

namespace BMTP3.Core2.BackupNew.Engine.Staging;

// Downloads item to a temp/staging file and replaces item.Content with staged content.
public interface IStagingDownloader
{
    Task DownloadToStagingAsync(IBackupItem item, string stagingRoot, IProgress<BackupProgress> progress, CancellationToken ct);
}