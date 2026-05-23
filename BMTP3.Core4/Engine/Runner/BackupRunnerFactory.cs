namespace BMTP3.Core4.Engine.Runner;

internal sealed class BackupRunnerFactory : IBackupRunnerFactory
{
	public IBackupRunner Create(BackupRunnerFactoryCreateRequest request)
	{
		return new SequentialBackupRunner();
	}
}
