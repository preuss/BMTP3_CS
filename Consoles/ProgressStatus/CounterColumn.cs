using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Fclp.Internals.Extensions;
using Spectre.Console;
using Spectre.Console.Extensions;
using Spectre.Console.Rendering;

namespace BMTP3_CS.Consoles.ProgressStatus {
	public class CounterColumn<T> : ProgressColumn where T : struct, INumber<T> {
		public CounterColumn(string key) {
			Key = key;
		}
		public CounterColumn(string key, T incrementSize = default, string? header = default) {
			Key = key;
			IncrementSize = incrementSize;
			Header = header;
		}
		protected override bool NoWrap => base.NoWrap;

		public string Key { get; }

		public T IncrementSize { get; set; }
		public string? Header { get; set; }
		public bool HeaderEmojiEscape { get; set; } = false;

		public Style? HeaderStyle { get; set; }

		public Style? CounterStyle { get; set; }

		internal static string ReplaceExact(string text, string oldValue, string? newValue) {
			return text.Replace(oldValue, newValue, StringComparison.Ordinal);
		}
		internal static string? RemoveNewLines(string? text) {
			if(text is null) {
				return null;
			}
			text = ReplaceExact(text, "\r\n", string.Empty);
			text = ReplaceExact(text, "\n", string.Empty);
			return text;
		}

		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime) {
			var header = RemoveNewLines(Header)?.Trim() ?? "";
			if(HeaderEmojiEscape) {
				header = Emoji.Replace(header);
			}

			var counter = task.State.Get<T>(Key);
			//return new Markup($"{text}: [bold]{counter}[/]");

			Paragraph paragraph = new Paragraph();
			if(string.IsNullOrEmpty(header)) {
				paragraph.Append(header, HeaderStyle);
				paragraph.Append(": ", HeaderStyle);
			}
			paragraph.Append($"{counter}", CounterStyle);

			return paragraph;
		}
	}
}
