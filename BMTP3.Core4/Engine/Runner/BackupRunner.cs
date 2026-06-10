using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;
using System.Text;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class BackupRunner : IBackupRunner
{
	public async Task<BackupResultItem> RunAsync(
		BackupRecord record,
		FileInfo tempFile,
		BackupRunnerRequest request,
		CancellationToken cancellationToken)
	{
		// NOTE: This runner is kept for reference but is no longer called
		// by BackupEngine. Collision resolution now happens via
		// ITargetPathResolver + ICollisionResolver in Engine/Strategies/.
		_ = cancellationToken;

		ArgumentNullException.ThrowIfNull(record);
		ArgumentNullException.ThrowIfNull(tempFile);
		ArgumentNullException.ThrowIfNull(request);

		string destinationDir = Path.GetDirectoryName(record.DestinationPath)
			?? throw new InvalidOperationException($"Cannot determine destination directory from '{record.DestinationPath}'.");

		string intendedPath = Path.Combine(destinationDir, record.Item.FileName);
		string finalPath;

		if(!File.Exists(intendedPath))
		{
			finalPath = intendedPath;
		} else if(request.CollisionStrategy == CollisionStrategy.Skip)
		{
			return ToResultItem(record.Item, destinationPath: null, BackupResultItemState.Skipped);
		} else if(request.CollisionStrategy == CollisionStrategy.Error)
		{
			throw new IOException($"Destination already exists: {intendedPath}");
		} else
		{
			finalPath = intendedPath;
		}

		Directory.CreateDirectory(destinationDir);

		FileInfo sidecarFile = new(tempFile.FullName + ".ini");
		await BuildAndSaveSidecarAsync(record, sidecarFile, request.SidecarFormat, request.BackupStartTime, cancellationToken);

		tempFile.MoveTo(finalPath, overwrite: request.CollisionStrategy == CollisionStrategy.Overwrite);

		string finalSidecarPath = finalPath + ".ini";
		if(sidecarFile.Exists)
		{
			sidecarFile.MoveTo(finalSidecarPath, overwrite: request.CollisionStrategy == CollisionStrategy.Overwrite);
		}

		return ToResultItem(record.Item, finalPath, BackupResultItemState.Succeeded);
	}

	private static async Task BuildAndSaveSidecarAsync(BackupRecord record, FileInfo sidecarFile, SidecarFormat sidecarFormat, DateTimeOffset backupStartTime, CancellationToken cancellationToken)
	{
		string content = sidecarFormat switch
		{
			SidecarFormat.Ini => BuildIniSidecarContent(record, backupStartTime),

			SidecarFormat.None or SidecarFormat.Json => throw new NotImplementedException($"Sidecar format '{sidecarFormat}' is not yet implemented."),

			_ => throw new InvalidOperationException($"Unexpected SidecarFormat '{sidecarFormat}'."),
		};

		await File.WriteAllTextAsync(sidecarFile.FullName, content, cancellationToken);
	}

	private static string BuildIniSidecarContent(BackupRecord record, DateTimeOffset backupStartTime)
	{
		var sb = new StringBuilder();

		sb.AppendLine("[Settings]");
		sb.AppendLine($"OriginalFileName={record.Item.FileName}");
		sb.AppendLine($"CreateDateTime={record.Item.DateCreated?.ToString("O")}");
		sb.AppendLine($"LastAccessDateTime={record.Item.DateAccessed?.ToString("O")}");
		sb.AppendLine($"LastWriteDateTime={record.Item.DateModified?.ToString("O")}");
		sb.AppendLine($"MediaTakenDateTime={record.Item.DateAuthored?.ToString("O")}");
		sb.AppendLine($"RelativeFilePath={record.Item.RelativeFilePath}");
		sb.AppendLine();

		sb.AppendLine("[BackupInfo]");
		sb.AppendLine($"BackupDateTime={backupStartTime:O}");
		sb.AppendLine();

		if(record.Metadata.ComputedHashes is { Count: > 0 })
		{
			sb.AppendLine("[Hashes]");
			foreach(KeyValuePair<HashType, string> hash in record.Metadata.ComputedHashes)
			{
				sb.AppendLine($"{hash.Key}={hash.Value}");
			}
		}

		return sb.ToString();
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
