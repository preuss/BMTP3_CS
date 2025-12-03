namespace BMTP3.Consoles.Services;
internal class SystemConsoleWriter : IConsoleWriter
{
	public void WriteLine(string? text = null)
	{
		Console.WriteLine(text);
	}

	public void Write(string? text = null)
	{
		Console.Write(text);
	}
}
