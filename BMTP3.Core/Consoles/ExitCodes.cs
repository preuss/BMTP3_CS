namespace BMTP3.Core.Consoles {
	internal static class ExitCodes {
		public const int Success = 0;           // Normal completion
		public const int UnknownCommand = -10;  // Argument parsing produced unknown
		public const int GenericError = -1;     // Generic recoverable error
		public const int FileNotFound = -2;     // Required file missing
		public const int FatalError = -99;      // Unhandled exception
		public const int UnhandledError = -100; // Initialization or unexpected failure
	}
}
