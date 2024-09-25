using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Consoles.Spinner {
	public sealed class SimpleSpinner : Spectre.Console.Spinner {
		// The interval for each frame
		public override TimeSpan Interval => TimeSpan.FromMilliseconds(100);

		// Whether or not the spinner contains unicode characters
		public override bool IsUnicode => false;

		// The individual frames of the spinner
		public override IReadOnlyList<string> Frames =>
			new List<string>
			{
			"A", "B", "C",
			};
	}
}

