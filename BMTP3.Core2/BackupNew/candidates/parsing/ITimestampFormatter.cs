namespace BMTP3.Core2.BackupNew.candidates.parsing;

public interface ITimestampFormatter
{
	string Format(TimestampCandidate candidate, TimestampFormatStyle formatStyle);
	string Format(TimestampCandidate candidate, TimestampFormatDescriptor descriptor);
}
