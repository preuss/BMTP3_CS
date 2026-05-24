namespace BMTP3.Core4.Engine.TimeStamp.Candidates.Parsing;

public interface ITimestampFormatter
{
	string Format(TimestampCandidate candidate, TimestampFormatStyle formatStyle);
	string Format(TimestampCandidate candidate, TimestampFormatDescriptor descriptor);
}