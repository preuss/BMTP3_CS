using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Options {
	public class ApplicationArguments : IArguments {
		public string? DefaultConfigurationFile { get; set; }
		public string? Backup { get; set; }
		public string? Verify { get; set; }
		public string? VerifyPath { get; set; }
		public bool Test { get; set; }
		public override string ToString() {
			return $"[ApplicationArguments: " +
				   $"DefaultConfigurationFile: {DefaultConfigurationFile ?? "null"}, " +
				   $"Backup: {Backup ?? "null"}, " +
				   $"Verify: {Verify ?? "null"}, " +
				   $"VerifyPath: {VerifyPath ?? "null"}, " +
				   $"Test: {Test}" +
				   $"]"
				   ;
		}
	}
}
