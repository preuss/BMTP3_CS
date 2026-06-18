using Spectre.Console;
using Spectre.Console.Rendering;

namespace BMTP3.Consoles.IO.Consoles.ProgressStatus;

/// <summary>
/// Renders value/max as formatted byte sizes.
/// Overall task: shows total bytes processed / total bytes selected (from State).
/// Per-file task: shows bytes read / file size (from task.Value / task.MaxValue).
/// Both sides are right-aligned with a fixed separator.
/// State keys: "IsByteTask" (bool), "TotalBytesProcessed" (long), "TotalBytesSelected" (long).
/// </summary>
public class ValueOfMaxColumn : ProgressColumn
{
	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		bool isByteTask = task.State.Get<bool>("IsByteTask");

		long left;
		long right;

		if(isByteTask)
		{
			left = (long)task.Value;
			right = (long)task.MaxValue;
		}
		else
		{
			left = task.State.Get<long>("TotalBytesProcessed");
			right = task.State.Get<long>("TotalBytesSelected");
		}

		if(right == 0)
			return new Markup($"[blue]{FormatBytes(left)}[/]");

		string leftStr = FormatBytes(left);
		string rightStr = FormatBytes(right);
		int width = Math.Max(leftStr.Length, rightStr.Length);

		return new Markup($"[blue]{leftStr.PadLeft(width)} / {rightStr.PadLeft(width)}[/]");
	}

	private static string FormatBytes(long bytes)
	{
		if(bytes < 1024L)
			return $"{bytes} B";

		if(bytes < 1024L * 1024L)
			return $"{bytes / 1024.0:F2} KB";

		if(bytes < 1024L * 1024L * 1024L)
			return $"{bytes / (1024.0 * 1024.0):F2} MB";

		return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
	}
}
