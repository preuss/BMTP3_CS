namespace BMTP3.Core2.Configs
{
	public static class RunCommandExtensions
	{
		public static bool IsBackup(this RunCommand runCommand)
		{
			return RunCommand.BACKUP == runCommand;
		}
	}
}
