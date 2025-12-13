using BMTP3.Core2.BackupNew.Domain.Item;
using MediaDevices;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;

// Converts a MediaFileInfo into a BackupItem with IContent wrapper (no heavy I/O).
public interface IMediaToBackupItemConverter
{
    BackupItem Convert(MediaFileInfo mediaInfo);
}