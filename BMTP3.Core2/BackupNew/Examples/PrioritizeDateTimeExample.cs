using BMTP3.Core2.BackupNew.Models;

namespace BMTP3.Core2.BackupNew.Examples;

public class PrioritizeDateTimeExample
{
	private (DateTimeOffset, TimestampSource) GetPrioritizedDateTime(DateTimeOffset? internalMetadata, DateTimeOffset? authoredDate, DateTimeOffset? creationTime, DateTimeOffset? lastWriteTime)
	{
		if(internalMetadata.HasValue && internalMetadata.Value > DateTimeOffset.MinValue)
		{
			return (internalMetadata.Value, TimestampSource.InternalMetadata);
		}
		if(authoredDate.HasValue && authoredDate.Value > DateTimeOffset.MinValue)
		{
			return (authoredDate.Value, TimestampSource.DeviceMetadata);
		}
		if(creationTime.HasValue && creationTime.Value > DateTimeOffset.MinValue)
		{
			return (creationTime.Value, TimestampSource.FilesystemCreation);
		}
		if(lastWriteTime.HasValue && lastWriteTime.Value > DateTimeOffset.MinValue)
		{
			return (lastWriteTime.Value, TimestampSource.FilesystemModification);
		}

		return (DateTimeOffset.Now, TimestampSource.CurrentTime); // Fallback
	}
}

