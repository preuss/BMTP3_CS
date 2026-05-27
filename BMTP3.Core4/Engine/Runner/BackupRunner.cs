using System.Text;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class BackupRunner : IBackupRunner
{
	public async Task<BackupResultItem> RunAsync(
		BackupRecord record,
		FileInfo tempFile,
		BackupRunnerRequest request,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(record);
		ArgumentNullException.ThrowIfNull(tempFile);
		ArgumentNullException.ThrowIfNull(request);

		string destinationDir = Path.GetDirectoryName(record.DestinationPath)
			?? throw new InvalidOperationException($"Cannot determine destination directory from '{record.DestinationPath}'.");

		string finalPath = CollisionHelpers.ResolveTargetPath(
			destinationDir,
			record.Item.FileName,
			request.CollisionStrategy,
			out CollisionResolution resolution);

		switch(resolution)
		{
			case CollisionResolution.Skip:
				tempFile.Delete();
				return ToResultItem(record.Item, destinationPath: null, BackupResultItemState.Skipped);

			case CollisionResolution.Error:
				tempFile.Delete();
				throw new IOException($"Destination already exists: {finalPath}");
		}

		Directory.CreateDirectory(destinationDir);

		FileInfo sidecarFile = new(tempFile.FullName + ".ini");
		await BuildAndSaveSidecarAsync(record, sidecarFile, request.SidecarFormat, request.BackupStartTime, cancellationToken);

		tempFile.MoveTo(finalPath, overwrite: resolution == CollisionResolution.Overwrite);

		string finalSidecarPath = finalPath + ".ini";
		if(sidecarFile.Exists)
		{
			sidecarFile.MoveTo(finalSidecarPath, overwrite: resolution == CollisionResolution.Overwrite);
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
		sb.AppendLine($"CreateDateTime={record.Metadata.CreatedDateTime?.ToString("O")}");
		sb.AppendLine($"LastAccessDateTime={record.Metadata.AccessedDateTime?.ToString("O")}");
		sb.AppendLine($"LastWriteDateTime={record.Metadata.ModifiedDateTime?.ToString("O")}");
		sb.AppendLine($"MediaTakenDateTime={record.Metadata.AuthoredDateTime?.ToString("O")}");
		sb.AppendLine($"RelativePath={record.Item.RelativePath}");
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
