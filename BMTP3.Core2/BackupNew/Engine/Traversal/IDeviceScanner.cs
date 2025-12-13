using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BMTP3.Core2.BackupNew.Domain.Item;
using MediaDevices;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;

// Streams device file infos (single-thread device access).
public interface IDeviceScanner
{
    IAsyncEnumerable<MediaFileInfo> ScanAsync(string sourceId, string sourcePath, bool recursive, CancellationToken ct);
}