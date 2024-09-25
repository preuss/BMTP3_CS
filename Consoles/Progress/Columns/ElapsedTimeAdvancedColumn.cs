using Spectre.Console.Rendering;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Consoles.Progress.Columns {
	public class ElapsedTimeAdvancedColumn : ProgressColumn {
		/// <inheritdoc/>
		protected override bool NoWrap => true;

		public bool ShowMilliseconds { get; set; } = true;

		/// <summary>
		/// Gets or sets the style of the remaining time text.
		/// </summary>
		public Style Style { get; set; } = Color.Blue;

		/// <inheritdoc/>
		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime) {
			var elapsed = task.ElapsedTime;
			if(elapsed == null) {
				if(ShowMilliseconds) {
					return new Markup("--:--:--.___");
				} else {
					return new Markup("--:--:--");
				}
			}

			if(elapsed.Value.TotalHours > 99) {
				if(ShowMilliseconds) {
					return new Markup("**:**:**.***");
				} else {
					return new Markup("**:**:**.***");
				}
			}

			if(ShowMilliseconds) {
				return new Text($"{elapsed.Value:hh\\:mm\\:ss\\.fff}", Style ?? Style.Plain);
			} else {
				return new Text($"{elapsed.Value:hh\\:mm\\:ss}", Style ?? Style.Plain);
			}
		}

		/// <inheritdoc/>
		public override int? GetColumnWidth(RenderOptions options) {
			if(ShowMilliseconds) {
				return 12;
			} else {
				return 8;
			}
		}
	}
}
