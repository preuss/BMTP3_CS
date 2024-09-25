using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console.Rendering;
using Spectre.Console;

namespace BMTP3_CS.Consoles.ProgressStatus {
	public class CustomTextColumn : ProgressColumn {
		private readonly string _header;
		private readonly Func<ProgressTaskState, string> _valueFunc;

		public CustomTextColumn(string header, Func<ProgressTaskState, string> valueFunc) {
			_header = header;
			_valueFunc = valueFunc;
		}

		protected override bool NoWrap => true;

		public string Header => _header;

		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime) {
			var value = _valueFunc(task.State);
			return new Markup(string.Format(_header, value));
		}

		/*
		public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
		{
			var value = _valueFunc(task.State);
			return new Markup(value);
		}
		*/
	}
}
