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
		bool isByteTask = task.State.Get<bool>(ProgressTaskStateKeys.IsByteTask);
		int raw = task.State.Get<int>(isByteTask ? ProgressTaskStateKeys.ActiveFilePhase : ProgressTaskStateKeys.OverallPhase);

		string phase = GetPhaseText(raw, isByteTask);

		if (phase.Length > Width)
			phase = phase[..Width];

		return new Text(phase, Style ?? Spectre.Console.Style.Plain);
	}

	internal static string GetPhaseText(int raw, bool isByteTask)
	{
		if (isByteTask)
		{
			return raw >= 0
				? ToShortLabel((BackupProgressItemPhase)raw)
				: string.Empty;
		}

		return ToShortLabel((BackupProgressPhase)raw);
	}

	internal static string ToShortLabel(BackupProgressPhase phase)
	{
		return phase switch
		{
			BackupProgressPhase.Starting => "Start",
			BackupProgressPhase.Scanning => "Scan",
			BackupProgressPhase.Transferring => "Transfer",
			BackupProgressPhase.Completed => "Done",
			_ => phase.ToString()
		};
	}

	internal static string ToShortLabel(BackupProgressItemPhase phase)
	{
		return phase switch
		{
			BackupProgressItemPhase.Transferring => "Transfer",
			BackupProgressItemPhase.ProcessingMetadata => "Meta",
			BackupProgressItemPhase.Hashing => "Hash",
			BackupProgressItemPhase.Finalizing => "Final",
			_ => phase.ToString()
		};
	}

	public override int? GetColumnWidth(RenderOptions options) => Width;
}
