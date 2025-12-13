using System;
using System.IO;
using MediaDevices;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;

/// <summary>
/// Converts a MediaFileInfo (from MediaDevices library) into a BackupItem with IContent wrapper.
/// No heavy I/O is performed; only metadata is extracted and wrapped.
/// </summary>
public class MediaToBackupItemConverter : IMediaToBackupItemConverter
{
    public BackupItem Convert(MediaFileInfo mediaInfo)
    {
        ArgumentNullException.ThrowIfNull(mediaInfo);

        // 1. Wrap MediaFileInfo in MediaFileContent (IContent)
        var content = new MediaFileContent(mediaInfo);

        // 2. Extract file name and relative path
        string fileName = mediaInfo.Name;
        string? directoryName = Path.GetDirectoryName(mediaInfo.FullName);
        string relativePath = string.IsNullOrWhiteSpace(directoryName) ? string.Empty : directoryName;

        // 3. Create BackupItem with initial metadata
        var item = BackupItem.Create(content, fileName, relativePath);

        // 4. Seed metadata from MediaFileInfo
        item.Metadata.Set(MetadataKey.SourceId, mediaInfo.PersistentUniqueId);
        item.Metadata.Set(MetadataKey.SourceFullPath, mediaInfo.FullName);
        item.Metadata.Set(MetadataKey.SourceRelativePath, relativePath);
        item.Metadata.Set(MetadataKey.SourceFileName, fileName);
        item.Metadata.Set(MetadataKey.Length, content.Length);

        // 5. Seed datetime metadata
        if (mediaInfo.DateAuthored.HasValue)
            item.Metadata.AuthoredDateTime = mediaInfo.DateAuthored.Value;
        if (mediaInfo.CreationTime.HasValue)
            item.Metadata.CreatedDateTime = mediaInfo.CreationTime.Value;
        if (mediaInfo.LastWriteTime.HasValue)
            item.Metadata.ModifiedDateTime = mediaInfo.LastWriteTime.Value;

        return item;
    }
}
