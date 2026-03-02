using Spectre.Console;
using Spectre.Console.Rendering;

namespace BMTP3.Core.IO.Consoles.ProgressStatus {
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
