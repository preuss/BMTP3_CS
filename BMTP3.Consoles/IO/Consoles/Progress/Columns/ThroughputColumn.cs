using Spectre.Console;
using Spectre.Console.Rendering;

namespace BMTP3.Consoles.IO.Consoles.Progress.Columns;

public class ThroughputColumn : ProgressColumn
{
	protected override bool NoWrap => true;

	public Style? Style { get; set; } = Color.Green;

	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		bool isByteTask = task.State.Get<bool>("IsByteTask");
		long bytes = isByteTask ? (long)task.Value : task.State.Get<long>("TotalBytesProcessed");
		TimeSpan? elapsed = task.ElapsedTime;

		if (elapsed == null || elapsed.Value.TotalSeconds < 1 || bytes <= 0)
			return new Text("--.- MB/s", Style ?? Spectre.Console.Style.Plain);

		double bytesPerSecond = bytes / elapsed.Value.TotalSeconds;
		double mbps = bytesPerSecond / (1024.0 * 1024.0);

		string text = mbps < 1.0
			? $"{bytesPerSecond / 1024.0:F0} KB/s"
			: $"{mbps:F1} MB/s";

		return new Text(text, Style ?? Spectre.Console.Style.Plain);
	}

	public override int? GetColumnWidth(RenderOptions options) => 10;
}
