using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Options {
	public interface IArguments {
		public string? DefaultConfigurationFile { get; }
		public string? Backup { get; }
		public string? Verify { get; }
		public string? VerifyPath { get; }
		public bool Test { get; }

	}
}
