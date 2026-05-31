namespace BMTP3.Core4.Engine.Runner;
internal interface IBackupRunnerFactory
{
	IBackupRunner Create(BackupRunnerFactoryCreateRequest request);
}
