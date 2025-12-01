using System.IO;
using BMTP3.Core2.BackupNew2.Interfaces;

namespace BMTP3.Core2.BackupNew2.Models.Internal;

public class FileSourceContent : ISourceContent
{
    private readonly FileInfo _fileInfo;

    public FileSourceContent(string path)
    {
        _fileInfo = new FileInfo(path);
    }

    public FileSourceContent(FileInfo fi)
    {
        _fileInfo = fi;
    }

    public string Name => _fileInfo.Name;

    public string OriginalPath => _fileInfo.FullName;

    public long SizeBytes => _fileInfo.Length;

    public Stream OpenReadStream()
    {
        // Open with Read sharing to avoid locking issues if possible
        return _fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }
}
