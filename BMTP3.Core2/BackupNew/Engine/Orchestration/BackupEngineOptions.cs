namespace BMTP3.Core2.BackupNew.Engine.Orchestration;

public class BackupEngineOptions
{
	public int RetryAttempts { get; set; } = 3;
	public int BaseBackoffMs { get; set; } = 200;
	public int DegreeOfParallelism { get; set; } = 0; // 0 means auto (Environment.ProcessorCount/2)

	// Channel Buffer Sizes
	public int ScanChannelCapacity { get; set; } = 128;
	public int ConvertChannelCapacity { get; set; } = 128;
	public int StagingChannelCapacity { get; set; } = 128;
	public int ProcessingChannelCapacity { get; set; } = 128;

	public bool DebugSingleThreaded { get; set; } = false;
}