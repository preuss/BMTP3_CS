namespace BMTP3.Core2.BackupNew.Engine.Orchestration;

public class BmtpOptions
{
	public int RetryAttempts { get; set; } = 3;
	public int BaseBackoffMs { get; set; } = 200;
	public int DegreeOfParallelism { get; set; } = 0; // 0 means auto (Environment.ProcessorCount/2)
}