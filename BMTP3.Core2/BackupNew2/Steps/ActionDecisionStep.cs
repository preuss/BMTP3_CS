using BMTP3.Common.MessageFormatterParser;
using BMTP3.Core2.BackupNew2.Interfaces;
using BMTP3.Core2.BackupNew2.Models;
using BMTP3.Core2.BackupNew2.Models.Configuration;
using BMTP3.Core2.BackupNew2.Models.Configuration.Enums;

namespace BMTP3.Core2.BackupNew2.Steps;

public class ActionDecisionStep : IBackupStep
{
	private readonly IBackupStateRepository _repository;
	private readonly IMessageFormatter _messageFormatter;

	public ActionDecisionStep(IBackupStateRepository repository, IMessageFormatter messageFormatter)
	{
		_repository = repository;
		_messageFormatter = messageFormatter;
	}

	public string Name => "ActionDecision";

	public Task ExecuteAsync(IBackupItem item, BackupJob job, CancellationToken ct)
	{
		item.State = BackupState.ActionDecided;

		// 1. History Check (Deduplication)
		string hash = item.Metadata.Get<string>(MetadataKey.HashSha256) ?? "";

		// 2. Calculate Target Path
		string targetRelativePath;
		var formatterValues = new Dictionary<string, object>();

		// Populate common metadata for formatter
		if(item.Metadata.Has(MetadataKey.AuthoredDateTime))
		{
			DateTime authoredDate = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);
			formatterValues["YYYY"] = authoredDate.ToString("yyyy");
			formatterValues["MM"] = authoredDate.ToString("MM");
			formatterValues["DD"] = authoredDate.ToString("dd");
			formatterValues["HH"] = authoredDate.ToString("HH");
			formatterValues["mm"] = authoredDate.ToString("mm");
			formatterValues["ss"] = authoredDate.ToString("ss");

			// Milliseconds and Microseconds
			formatterValues["fff"] = authoredDate.ToString("fff");
			formatterValues["SSS"] = authoredDate.ToString("fff"); // Alias per spec
			formatterValues["ffffff"] = authoredDate.ToString("ffffff");
			formatterValues["fffffffff"] = authoredDate.ToString("fffffff00"); // .NET precision limit is 100ns

			formatterValues["date"] = authoredDate; // For direct date formatting in pattern like ${date:yyyy-MM-dd}
		}

		if(item.Metadata.Has(MetadataKey.OriginalFileName))
		{
			string originalFileName = item.Metadata.Get<string>(MetadataKey.OriginalFileName)!;
			formatterValues["originalName"] = Path.GetFileNameWithoutExtension(originalFileName);

			string ext = Path.GetExtension(originalFileName)?.TrimStart('.') ?? "";
			formatterValues["ext"] = ext;
			formatterValues["originalExtension"] = ext; // Backward compat / alias

			formatterValues["originalFullName"] = originalFileName;
		}

		if(item.Metadata.Has(MetadataKey.HashSha256))
		{
			string fullHash = item.Metadata.Get<string>(MetadataKey.HashSha256)!;
			formatterValues["hashLong"] = fullHash;
			formatterValues["hashFull"] = fullHash; // Alias
			formatterValues["hashMedium"] = fullHash.Length > 12 ? fullHash.Substring(0, 12) : fullHash;
			formatterValues["hashShort"] = fullHash.Length > 8 ? fullHash.Substring(0, 8) : fullHash;
		}

		if(item.Metadata.Has(MetadataKey.SourceRelativePath))
		{
			string relPath = item.Metadata.Get<string>(MetadataKey.SourceRelativePath)!;
			formatterValues["relativePath"] = relPath;
			formatterValues["sourceRelativePath"] = relPath; // Alias
		}

		if(item.Metadata.Has(MetadataKey.OriginalSourceId))
		{
			formatterValues["deviceName"] = item.Metadata.Get<string>(MetadataKey.OriginalSourceId) ?? "unknown";
		}

		switch(job.OutputStrategy)
		{
			case OutputStructureStrategy.Flat:
				targetRelativePath = item.Metadata.Get<string>(MetadataKey.OriginalFileName) ?? "unknown";
				break;
			case OutputStructureStrategy.PreserveSourceTree:
				var sourceRelativePath = item.Metadata.Get<string>(MetadataKey.SourceRelativePath);
				var originalFileName = item.Metadata.Get<string>(MetadataKey.OriginalFileName);
				targetRelativePath = sourceRelativePath ?? originalFileName ?? "unknown";
				break;
			case OutputStructureStrategy.CustomPathPattern:
				if(string.IsNullOrEmpty(job.CustomOutputPathPattern))
				{
					item.Fail("CustomOutputPathPattern is required for CustomPathPattern strategy.", Name);
					return Task.CompletedTask;
				}
				try
				{
					targetRelativePath = _messageFormatter.Format(job.CustomOutputPathPattern, formatterValues);
				} catch(Exception ex)
				{
					item.Fail($"Error formatting custom path pattern: {ex.Message}", Name, ex);
					return Task.CompletedTask;
				}
				break;
			default:
				item.Fail($"Unsupported output strategy: {job.OutputStrategy}", Name);
				return Task.CompletedTask;
		}

		string fullTargetPath = Path.Combine(job.OutputPath, targetRelativePath);

		// 3. Collision Detection & Resolution
		if(File.Exists(fullTargetPath))
		{
			// Handle Collision
			ResolveCollision(item, job, fullTargetPath, formatterValues); // Pass formatterValues for rename pattern
		} else
		{
			// No collision
			item.Action = BackupActionType.Copy;
			item.Metadata.Set(MetadataKey.FinalTargetPath, fullTargetPath);
		}

		return Task.CompletedTask;
	}

	private void ResolveCollision(IBackupItem item, BackupJob job, string initialTarget, Dictionary<string, object> formatterValues)
	{
		switch(job.CollisionResolution)
		{
			case CollisionResolutionType.Skip:
				item.Action = BackupActionType.Skip;
				item.Metadata.Set(MetadataKey.FinalTargetPath, initialTarget);
				break;

			case CollisionResolutionType.Overwrite:
				item.Action = BackupActionType.Copy;
				item.Metadata.Set(MetadataKey.FinalTargetPath, initialTarget);
				break;

			case CollisionResolutionType.Rename:
				string folder = Path.GetDirectoryName(initialTarget)!;
				string filename = Path.GetFileNameWithoutExtension(initialTarget);
				string ext = Path.GetExtension(initialTarget);

				// Add collision count to formatter values
				int count = 1;
				string newPath;
				do
				{
					// Add current count to formatter values
					formatterValues["count"] = count;
					string renamePattern = job.RenameStrategy == RenameStrategy.Increment
										 ? $"{filename}_{formatterValues["count"]}{ext}"
										 : _messageFormatter.Format(job.CustomCollisionPathPattern ?? $"{filename}_{formatterValues["count"]}{ext}", formatterValues);

					newPath = Path.Combine(folder, renamePattern);
					count++;
				} while(File.Exists(newPath));

				item.Action = BackupActionType.Rename;
				item.Metadata.Set(MetadataKey.FinalTargetPath, newPath);
				break;

			case CollisionResolutionType.Error:
			default:
				item.Fail($"File exists at {initialTarget}", Name);
				break;
		}
	}
}
