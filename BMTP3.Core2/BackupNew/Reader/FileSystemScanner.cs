using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Reader;
/// <summary>
/// Scans a local file system directory and enumerates all FileInfo entries.
/// </summary>
public class FileSystemScanner : IFileSourceScanner<FileInfo>
{
	private readonly DirectoryInfo _root;

	public FileSystemScanner(string rootPath)
	{
		if(string.IsNullOrWhiteSpace(rootPath))
		{
			throw new ArgumentException("Root path is required.", nameof(rootPath));
		}

		_root = new DirectoryInfo(rootPath);
	}

	public IEnumerable<FileInfo> ScanAll()
	{
		return ScanRecursive(_root);
	}

	private IEnumerable<FileInfo> ScanRecursive(DirectoryInfo dir)
	{
		foreach(var file in dir.GetFiles())
		{
			yield return file;
		}

		foreach(var subDir in dir.GetDirectories())
		{
			foreach(var file in ScanRecursive(subDir))
			{
				yield return file;
			}
		}
	}
}
