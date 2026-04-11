namespace BMTP3.Consoles.Services;

public interface IConsoleWriter
{
	void WriteLine(string? text = null);
	void Write(string? text = null);
}