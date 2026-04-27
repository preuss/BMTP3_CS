using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;

namespace BMTP3.Core2.BackupNew.Engine.Steps.TimestampCorrectionStep;

/// <summary>
///     Step for correcting the file system timestamp of the buffered/staged file
///     to match the extracted metadata (e.g., Date Taken).
/// </summary>
public class TimestampCorrectionItemStep : IBackupItemStep<BackupPlan, bool>
{
	/// <summary>
	///     Initializes a new instance of <see cref="TimestampCorrectionItemStep" />.
	/// </summary>
	/// <param name="context">The backup plan context for this step.</param>
	public TimestampCorrectionItemStep(BackupPlan context)
	{
		Context = context ?? throw new ArgumentNullException(nameof(context));
	}

   public string Name => "Timestamp Correction";
   public FilePhase Phase => FilePhase.None; // No direct match in new model
	public BackupPlan Context { get; }

	public Task<bool> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct)
	{
		DateTimeOffset? timestamp = null;

		if (item.Metadata.Has(MetadataKey.AuthoredDateTime))
		{
			timestamp = item.Metadata.Get<DateTimeOffset>(MetadataKey.AuthoredDateTime);
		}
		else if (item.Metadata.Has(MetadataKey.CreatedDateTime))
		{
			timestamp = item.Metadata.Get<DateTimeOffset>(MetadataKey.CreatedDateTime);
		}
		else if (item.Metadata.Has(MetadataKey.ModifiedDateTime))
		{
			timestamp = item.Metadata.Get<DateTimeOffset>(MetadataKey.ModifiedDateTime);
		}

		if (timestamp.HasValue)
		{
			if (item.Content is FileContent fileContent)
			{
				try
				{
					DateTime utcTime = timestamp.Value.UtcDateTime;
					File.SetLastWriteTimeUtc(fileContent.FileInfo.FullName, utcTime);
					File.SetCreationTimeUtc(fileContent.FileInfo.FullName, utcTime);
				}
				catch (Exception ex)
				{
					item.AddLog($"Failed to apply timestamp: {ex.Message}", Name);
				}
			}
		}

		return Task.FromResult(true);
	}
}