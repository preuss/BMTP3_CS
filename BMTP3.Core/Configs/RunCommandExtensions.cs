namespace BMTP3.Core.Configs {
	public static class RunCommandExtensions {
		public static bool IsBackup(this RunCommand runCommand) {
			return RunCommand.BACKUP == runCommand;
		}
	}
}
