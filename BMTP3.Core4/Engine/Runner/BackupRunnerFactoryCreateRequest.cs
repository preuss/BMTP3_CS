namespace BMTP3.Core4.Engine.Runner;
internal record BackupRunnerFactoryCreateRequest
{
	public int? MaxDegreeOfParallelism { get; init; }
}
