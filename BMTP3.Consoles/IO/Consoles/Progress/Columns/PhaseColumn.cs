using BMTP3.Core4.Api.Models.Enums;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BMTP3.Consoles.IO.Consoles.Progress.Columns;

public class PhaseColumn : ProgressColumn
{
	private const int Width = 7;

	protected override bool NoWrap => true;

	public Style? Style { get; set; } = Color.Yellow;

	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		string phase;

		if (task.State.Get<bool>("IsByteTask"))
		{
			int raw = task.State.Get<int>("ActiveFilePhase");
			phase = raw >= 0
				? ((BackupProgressItemPhase)raw).ToString()
				: string.Empty;
		}
		else
		{
			phase = ((BackupProgressPhase)task.State.Get<int>("OverallPhase")).ToString();
		}

		if (phase.Length > Width)
			phase = phase[..Width];

		return new Text(phase, Style ?? Spectre.Console.Style.Plain);
	}

	public override int? GetColumnWidth(RenderOptions options) => Width;
}
