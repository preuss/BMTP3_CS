using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MediaDevices;

namespace BMTP3.Core2.BackupNew.Engine.Traversal
{
    // Safe no-op device scanner that returns no entries.
    public class NoopDeviceScanner : IDeviceScanner
    {
		public async IAsyncEnumerable<MediaFileInfo> ScanAsync(
            string sourceId,
            string sourcePath,
            bool recursive,
            [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}