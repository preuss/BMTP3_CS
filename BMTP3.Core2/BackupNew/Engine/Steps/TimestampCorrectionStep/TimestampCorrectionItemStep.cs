using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Content;
using BMTP3.Core2.BackupNew.Domain.Item;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.TimestampCorrectionStep;

/// <summary>
/// Step for correcting the file system timestamp of the buffered/staged file
/// to match the extracted metadata (e.g., Date Taken).
/// </summary>
public class TimestampCorrectionItemStep : IBackupItemStep<BackupPlan, bool>
{
	public string Name => "Timestamp Correction";

	private readonly BackupPlan _context;
	public BackupPlan Context => _context;

	/// <summary>
	/// Initializes a new instance of <see cref="TimestampCorrectionItemStep"/>.
	/// </summary>
	/// <param name="context">The backup plan context for this step.</param>
	public TimestampCorrectionItemStep(BackupPlan context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	public Task<bool> ExecuteAsync(IBackupItem item, CancellationToken ct)
	{
		DateTime? timestamp = null;

		if (item.Metadata.Has(MetadataKey.AuthoredDateTime))
			timestamp = item.Metadata.Get<DateTime>(MetadataKey.AuthoredDateTime);
		else if (item.Metadata.Has(MetadataKey.CreatedDateTime))
			timestamp = item.Metadata.Get<DateTime>(MetadataKey.CreatedDateTime);
		else if (item.Metadata.Has(MetadataKey.ModifiedDateTime))
			timestamp = item.Metadata.Get<DateTime>(MetadataKey.ModifiedDateTime);

		if (timestamp.HasValue)
		{
			if (item.Content is FileContent fileContent)
			{
				try
				{
					File.SetLastWriteTimeUtc(fileContent.FileInfo.FullName, timestamp.Value.ToUniversalTime());
					File.SetCreationTimeUtc(fileContent.FileInfo.FullName, timestamp.Value.ToUniversalTime());
				}
				catch (Exception)
				{
					// Logging or error handling can be added here if needed.
				}
			}
		}

		return Task.FromResult(true);
	}
}
