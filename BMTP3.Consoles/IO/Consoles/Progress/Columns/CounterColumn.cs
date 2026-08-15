using BMTP3.Consoles.Extensions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BMTP3.Consoles.IO.Consoles.Progress.Columns;

public class CounterColumn : ProgressColumn
{
	/// <inheritdoc/>
	protected override bool NoWrap => true;

	/// <summary>
	/// Gets or sets the alignment of the task description.
	/// </summary>
	public Justify Alignment { get; set; } = Justify.Right;

	/// <inheritdoc/>
	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		return new Markup(RenderText(task.Description)).Overflow(Overflow.Ellipsis).Justify(Alignment);
	}

	internal static string RenderText(string? text)
	{
		return (text?.RemoveNewLines()?.Trim() ?? string.Empty).EscapeMarkup();
	}
}
