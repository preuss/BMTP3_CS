using Spectre.Console;
using Spectre.Console.Rendering;

namespace BMTP3.Consoles.IO.Consoles.Progress.Columns;

public class ElapsedTimeAdvancedColumn : ProgressColumn
{
	/// <inheritdoc/>
	protected override bool NoWrap => true;

	public bool ShowMilliseconds { get; set; } = true;

	/// <summary>
	/// Gets or sets the style of the elapsed time text.
	/// </summary>
	public Style? Style { get; set; } = Color.Blue;

	/// <inheritdoc/>
	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		TimeSpan? elapsed = task.ElapsedTime;
		Style style = this.Style ?? Spectre.Console.Style.Plain;

		if(elapsed == null)
		{
			if(ShowMilliseconds)
			{
				return new Text("--:--:--.___", style);
			} else
			{
				return new Text("--:--:--", style);
			}
		}

		int hh = (int)elapsed.Value.TotalHours;

		if(hh > 99)
		{
			if(ShowMilliseconds)
			{
				return new Text("**:**:**.***", style);
			} else
			{
				return new Text("**:**:**", style);
			}
		}

		if(ShowMilliseconds)
		{

			return new Text($"{hh:00}:{elapsed.Value:mm\\:ss\\.fff}", style);
		} else
		{
			return new Text($"{hh:00}:{elapsed.Value:mm\\:ss}", style);
		}
	}

	/// <inheritdoc/>
	public override int? GetColumnWidth(RenderOptions options)
	{
		if(ShowMilliseconds)
		{
			return 12;
		} else
		{
			return 8;
		}
	}
}