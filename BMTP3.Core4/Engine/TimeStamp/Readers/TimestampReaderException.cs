namespace BMTP3.Core4.Engine.TimeStamp.Readers;

public sealed class TimestampReaderException : Exception
{
	public Type ReaderType { get; }

	public TimestampReaderException(Type readerType, string message, Exception inner)
		: base($"[{readerType.Name}] {message}", inner)
	{
		ReaderType = readerType;
	}
}
