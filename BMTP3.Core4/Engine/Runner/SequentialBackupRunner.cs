using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class SequentialBackupRunner : IBackupRunner
{
	public async Task<BackupResultItem> RunAsync(
		BackupItem item,
		string destinationPath,
		string tempFilePath,
		BackupRunnerRequest request,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNull(destinationPath);
		ArgumentNullException.ThrowIfNull(tempFilePath);
		ArgumentNullException.ThrowIfNull(request);

		string? tempSidecarPath = null;

		try
		{
			string? parentDir = Path.GetDirectoryName(destinationPath);
			if(!string.IsNullOrEmpty(parentDir))
			{
				Directory.CreateDirectory(parentDir);
			}

			string finalPath = ResolveTargetPath(destinationPath, request.CollisionStrategy);

			tempSidecarPath = await BuildAndSaveSidecarAsync(item, tempFilePath, request.SidecarFormat, request.BackupStartTime, cancellationToken);

			CommitTransfer(tempFilePath, tempSidecarPath, finalPath);
			tempSidecarPath = null;

			return ToResultItem(item, finalPath, BackupResultItemState.Succeeded);
		}
		catch
		{
			CleanupTempFiles(tempFilePath, tempSidecarPath);
			throw;
		}
	}

	private static async Task<string> BuildAndSaveSidecarAsync(BackupItem item, string tempPath, SidecarFormat sidecarFormat, DateTimeOffset backupStartTime, CancellationToken cancellationToken)
	{
		string content = sidecarFormat switch
		{
			SidecarFormat.Ini => BuildIniSidecarContent(item, backupStartTime),

			SidecarFormat.None or SidecarFormat.Json => throw new NotImplementedException($"Sidecar format '{sidecarFormat}' is not yet implemented."),

			_ => throw new InvalidOperationException($"Unexpected SidecarFormat '{sidecarFormat}'."),
		};

		string sidecarPath = tempPath + ".ini";
		await File.WriteAllTextAsync(sidecarPath, content, cancellationToken);
		return sidecarPath;
	}

	private static string BuildIniSidecarContent(BackupItem item, DateTimeOffset backupStartTime)
	{
		return
			$"[Settings]{Environment.NewLine}" +
			$"OriginalFileName={item.FileName}{Environment.NewLine}" +
			$"CreateDateTime={item.DateCreated?.ToString("O")}{Environment.NewLine}" +
			$"LastAccessDateTime={item.DateAccessed?.ToString("O")}{Environment.NewLine}" +
			$"LastWriteDateTime={item.DateModified?.ToString("O")}{Environment.NewLine}" +
			$"MediaTakenDateTime={item.DateAuthored?.ToString("O")}{Environment.NewLine}" +
			$"RelativePath={item.RelativePath}{Environment.NewLine}" +
			$"{Environment.NewLine}" +
			$"[BackupInfo]{Environment.NewLine}" +
			$"BackupDateTime={backupStartTime:O}{Environment.NewLine}";
	}

	private static string ResolveTargetPath(string destinationPath, CollisionStrategy collisionStrategy)
	{
		if(!File.Exists(destinationPath))
		{
			return destinationPath;
		}

		switch(collisionStrategy)
		{
			case CollisionStrategy.Error:
				throw new IOException($"Destination already exists: {destinationPath}");

			default:
				throw new InvalidOperationException($"Collision strategy '{collisionStrategy}' is not supported in the current Tier.");
		}
	}

	private static void CommitTransfer(string tempPath, string tempSidecarPath, string finalPath)
	{
		File.Move(tempPath, finalPath, overwrite: false);

		string finalSidecarPath = finalPath + ".ini";
		if(File.Exists(tempSidecarPath))
		{
			File.Move(tempSidecarPath, finalSidecarPath, overwrite: false);
		}
	}

	private static void CleanupTempFiles(string? tempPath, string? tempSidecarPath)
	{
		if(tempPath is not null && File.Exists(tempPath))
		{
			try { File.Delete(tempPath); } catch { }
		}

		if(tempSidecarPath is not null && File.Exists(tempSidecarPath))
		{
			try { File.Delete(tempSidecarPath); } catch { }
		}
	}

	private static BackupResultItem ToResultItem(BackupItem item, string? destinationPath, BackupResultItemState state)
	{
		return new BackupResultItem
		{
			Id = item.Id,
			SourcePath = item.SourcePath,
			DestinationPath = destinationPath,
			Length = (long)item.Content.Length,
			State = state,
		};
	}
}
