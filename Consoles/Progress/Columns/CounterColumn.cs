using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMTP3_CS.Extensions;

namespace BMTP3_CS.Consoles.Progress.Columns {
	public class CounterColumn : ProgressColumn {
		/// <inheritdoc/>
		protected override bool NoWrap => true;

		/// <summary>
		/// Gets or sets the alignment of the task description.
		/// </summary>
		public Justify Alignment { get; set; } = Justify.Right;

		/// <inheritdoc/>
		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime) {
			var text = task.Description?.RemoveNewLines()?.Trim();
			return new Markup(text ?? string.Empty).Overflow(Overflow.Ellipsis).Justify(Alignment);
		}
	}
}
