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
		string? text = task.Description?.RemoveNewLines()?.Trim();
		return new Markup(text ?? string.Empty).Overflow(Overflow.Ellipsis).Justify(Alignment);
	}
}
