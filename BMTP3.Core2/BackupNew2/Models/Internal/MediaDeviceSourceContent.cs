using System;
using System.IO;
using System.Runtime.Versioning;
using BMTP3.Core2.BackupNew2.Interfaces;
using MediaDevices;

namespace BMTP3.Core2.BackupNew2.Models.Internal;

[SupportedOSPlatform("windows7.0")]
public class MediaDeviceSourceContent : ISourceContent
{
    private readonly MediaFileInfo _mediaFileInfo;

    public MediaDeviceSourceContent(MediaFileInfo mediaFileInfo)
    {
        _mediaFileInfo = mediaFileInfo ?? throw new ArgumentNullException(nameof(mediaFileInfo));
    }

    public string Name => _mediaFileInfo.Name;

    public string OriginalPath => _mediaFileInfo.FullName;

    // Cast ulong to long. In practice, file sizes won't exceed long.MaxValue (9 EB) soon.
    public long SizeBytes => (long)_mediaFileInfo.Length;

    public Stream OpenReadStream()
    {
        return _mediaFileInfo.OpenRead();
    }
}
