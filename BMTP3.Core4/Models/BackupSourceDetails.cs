namespace BMTP3.Core4.Models;

internal abstract record BackupSourceDetails
{
	public required string DriveName { get; init; }
	public required string VolumeLabel { get; init; }
	public required string DriveFormat { get; init; }
}
