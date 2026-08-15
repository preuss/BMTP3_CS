using BMTP3.Consoles.Extensions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace BMTP3.Consoles.IO.Consoles.Progress.Columns;

public class FileNameColumn : ProgressColumn
{
	private const int MaxWidth = 40;

	protected override bool NoWrap => true;

	public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
	{
		string text = task.Description?.RemoveNewLines()?.Trim() ?? "";

		if (text.Length > MaxWidth)
			text = ShortenPath(text, MaxWidth);

		return new Markup(text.EscapeMarkup())
			.Overflow(Overflow.Ellipsis)
			.Justify(Justify.Left);
	}

	public override int? GetColumnWidth(RenderOptions options) => MaxWidth + 3;

	internal static string ShortenPath(string path, int maxLength, string spacer = "...")
	{
		if (string.IsNullOrWhiteSpace(path) || maxLength <= 0)
			return string.Empty;

		if (path.Length <= maxLength)
			return path;

		char sep = path.Contains('\\') ? '\\' : '/';
		string[] parts = path.Split('\\', '/');

		string ext = Path.GetExtension(parts[^1]);
		string stem = Path.GetFileNameWithoutExtension(parts[^1]);
		string[] dirs = parts[..^1];

		// Step 1: try to fit dirs + truncated stem + ext within maxLength
		// Truncate stem from the middle first, keeping dirs intact
		string dirsStr = dirs.Length > 0 ? string.Join(sep, dirs) + sep : string.Empty;
		int stemBudget = maxLength - dirsStr.Length - ext.Length;

		if (stemBudget >= stem.Length)
		{
			// Stem fits as-is — no truncation needed
			return dirsStr + stem + ext;
		}

		if (stemBudget >= spacer.Length + 2)
		{
			// Truncate stem from middle
			return dirsStr + TruncateMiddle(stem, stemBudget, spacer) + ext;
		}

		// Step 2: stem budget too small — start truncating dirs from the end, one by one
		string[] truncDirs = (string[])dirs.Clone();

		for (int i = 0; i < truncDirs.Length; i++)
		{
			// Shrink dirs[i] from the end until we have enough budget for stem
			while (truncDirs[i].Length > spacer.Length)
			{
				truncDirs[i] = truncDirs[i][..^1];
				dirsStr = string.Join(sep, truncDirs.Select((d, idx) => idx == i ? d + spacer : d)) + sep;
				stemBudget = maxLength - dirsStr.Length - ext.Length;

				if (stemBudget >= spacer.Length + 2)
					return dirsStr + TruncateMiddle(stem, stemBudget, spacer) + ext;
			}

			// Collapsed to just spacer
			truncDirs[i] = spacer;
			dirsStr = string.Join(sep, truncDirs) + sep;
			stemBudget = maxLength - dirsStr.Length - ext.Length;

			if (stemBudget >= spacer.Length + 2)
				return dirsStr + TruncateMiddle(stem, stemBudget, spacer) + ext;
		}

		// Step 3: fallback — just show as much of stem as possible with ext
		int fallbackBudget = maxLength - ext.Length;
		if (fallbackBudget >= spacer.Length + 2)
			return TruncateMiddle(stem, fallbackBudget, spacer) + ext;

		return (spacer + ext)[..Math.Min(maxLength, spacer.Length + ext.Length)];
	}

	internal static string TruncateMiddle(string text, int maxLength, string spacer = "...")
	{
		if (text.Length <= maxLength)
			return text;

		int available = maxLength - spacer.Length;
		if (available <= 0)
			return spacer[..Math.Min(spacer.Length, maxLength)];

		// Distribute evenly: slightly more at the start
		int startLen = (available + 1) / 2;
		int endLen = available / 2;

		return text[..startLen] + spacer + text[^endLen..];
	}
}
