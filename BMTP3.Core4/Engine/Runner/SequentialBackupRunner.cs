using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class SequentialBackupRunner : IBackupRunner
{
	public async Task<BackupResultItem> RunAsync(
		BackupItem item,
		FileInfo destinationFile,
		FileInfo tempFile,
		BackupRunnerRequest request,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNull(destinationFile);
		ArgumentNullException.ThrowIfNull(tempFile);
		ArgumentNullException.ThrowIfNull(request);

		destinationFile.Directory?.Create();

		string finalPath = ResolveTargetPath(destinationFile.FullName, request.CollisionStrategy);

		FileInfo sidecarFile = new(tempFile.FullName + ".ini");
		await BuildAndSaveSidecarAsync(item, sidecarFile, request.SidecarFormat, request.BackupStartTime, cancellationToken);

		tempFile.MoveTo(finalPath, overwrite: false);

		string finalSidecarPath = finalPath + ".ini";
		if(sidecarFile.Exists)
		{
			sidecarFile.MoveTo(finalSidecarPath, overwrite: false);
		}

		return ToResultItem(item, finalPath, BackupResultItemState.Succeeded);
	}

	private static async Task BuildAndSaveSidecarAsync(BackupItem item, FileInfo sidecarFile, SidecarFormat sidecarFormat, DateTimeOffset backupStartTime, CancellationToken cancellationToken)
	{
		string content = sidecarFormat switch
		{
			SidecarFormat.Ini => BuildIniSidecarContent(item, backupStartTime),

			SidecarFormat.None or SidecarFormat.Json => throw new NotImplementedException($"Sidecar format '{sidecarFormat}' is not yet implemented."),

			_ => throw new InvalidOperationException($"Unexpected SidecarFormat '{sidecarFormat}'."),
		};

		await File.WriteAllTextAsync(sidecarFile.FullName, content, cancellationToken);
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
