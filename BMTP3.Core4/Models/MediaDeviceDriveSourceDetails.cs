namespace BMTP3.Core4.Models;

internal sealed record MediaDeviceDriveSourceDetails : BackupSourceDetails
{
	public required string DeviceId { get; init; }
	public required string Description { get; init; }
	public required string FriendlyName { get; init; }
	public required string Manufacturer { get; init; }
	public required string Model { get; init; }
	public required string SerialNumber { get; init; }
	public required string FirmwareVersion { get; init; }
}
