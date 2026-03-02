namespace BMTP3.Core.Options {
	public interface IArguments {
		public string? DefaultConfigurationFile { get; }
		public string? Backup { get; }
		public string? Verify { get; }
		public string? VerifyPath { get; }
		public bool Test { get; }

	}
}
