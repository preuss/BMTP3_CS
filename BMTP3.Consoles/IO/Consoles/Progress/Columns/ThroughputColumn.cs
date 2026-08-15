using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BMTP3.Consoles.IO.Consoles.Progress.Columns;

public class ThroughputColumn : ProgressColumn
{
	protected override bool NoWrap => true;

	public Style? Style { get; set; } = Color.Green;

	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		bool isByteTask = task.State.Get<bool>(ProgressTaskStateKeys.IsByteTask);
		long bytes = isByteTask ? (long)task.Value : task.State.Get<long>(ProgressTaskStateKeys.TotalBytesProcessed);

		return new Text(FormatThroughput(bytes, task.ElapsedTime), Style ?? Spectre.Console.Style.Plain);
	}

	internal static string FormatThroughput(long bytes, TimeSpan? elapsed)
	{
		if (elapsed == null || elapsed.Value.TotalSeconds < 1 || bytes <= 0)
			return "--.- MB/s";

		double bytesPerSecond = bytes / elapsed.Value.TotalSeconds;
		double mbps = bytesPerSecond / (1024.0 * 1024.0);

		return mbps < 1.0
			? $"{(bytesPerSecond / 1024.0).ToString("F0", CultureInfo.InvariantCulture)} KB/s"
			: $"{mbps.ToString("F1", CultureInfo.InvariantCulture)} MB/s";
	}

	public override int? GetColumnWidth(RenderOptions options) => 10;
}
