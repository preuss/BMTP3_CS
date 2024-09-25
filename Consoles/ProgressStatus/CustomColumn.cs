using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console.Rendering;
using Spectre.Console;

namespace BMTP3_CS.Consoles.ProgressStatus {
	public class CustomColumn : ProgressColumn {
		private readonly string _header;
		private readonly Func<string> _valueFunc;

		public CustomColumn(string header, Func<string> valueFunc) {
			_header = header;
			_valueFunc = valueFunc;
		}

		protected override bool NoWrap => true;

		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime) {
			return new Markup($"{_header}: [bold]{_valueFunc()}[/]");
		}

		public string Header => _header;
	}
}
