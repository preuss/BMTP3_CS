using System;
using System.Collections.Generic;
using System.IO;
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

	public IEnumerable<FileInfo> TraverseFiles(bool recursive = true, Events.TraversalProgressCounter? progress = null, CancellationToken cancellationToken = default)
	{
		return Traverse(_root, recursive, progress, cancellationToken);
	}

	private IEnumerable<FileInfo> Traverse(DirectoryInfo dir, bool recursive, Events.TraversalProgressCounter? progress, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		foreach (var file in dir.GetFiles())
		{
			progress?.IncrementFileCount();
			yield return file;
		}

		if (recursive)
		{
			foreach (var subDir in dir.GetDirectories())
			{
				progress?.IncrementDirectoryCount();
				foreach (var file in Traverse(subDir, true, progress))
				{
					yield return file;
				}
			}
		}
	}
}
