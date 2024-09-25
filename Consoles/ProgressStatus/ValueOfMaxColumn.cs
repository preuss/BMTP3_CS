using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Consoles.ProgressStatus {
	public class ValueOfMaxColumn : ProgressColumn {
		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime) {
			return new Markup($"[blue]{task.Value}/{task.MaxValue}[/]");
		}
	}
}
