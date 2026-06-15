namespace BMTP3.Core4.Api.Models;

public sealed record BackupResultCounts
{
	public int Total { get; init; }
	public int Succeeded { get; init; }
	public int Failed { get; init; }
	public int Skipped { get; init; }
}
