using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Storage;

internal sealed class BackupFileSystemDriveInfo : IBackupFileSystemDriveInfo
{
	private readonly DriveInfo _drive;

	public BackupFileSystemDriveInfo(DriveInfo drive)
	{
		_drive = drive ?? throw new ArgumentNullException(nameof(drive));

		if(!drive.IsReady)
		{
			throw new InvalidOperationException($"Drive '{drive.Name}' is not ready and cannot be used to create {nameof(BackupFileSystemDriveInfo)}.");
		}

		DriveName = BuildDriveName(drive);
		DisplayName = BuildDisplayName(drive);
		RootPath = BuildRootPath(drive);
		TotalSize = drive.TotalSize;
		AvailableFreeSpace = drive.AvailableFreeSpace;
		VolumeLabel = drive.VolumeLabel;
		DriveFormat = drive.DriveFormat;
		DriveType = drive.DriveType;
	}

	internal DriveInfo DriveInfo => _drive;

	public string Id => DriveName;

	public string DriveName { get; }

	public string DisplayName { get; }

	public BackupSourceType SourceType => BackupSourceType.FileSystem;

	public string RootPath { get; }

	public long TotalSize { get; }

	public long AvailableFreeSpace { get; }

	public string VolumeLabel { get; }

	public string DriveFormat { get; }

	public DriveType DriveType { get; }

	private static string BuildDriveName(DriveInfo drive)
	{
		return drive.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	private static string BuildDisplayName(DriveInfo drive)
	{
		string driveName = BuildDriveName(drive);

		if(!string.IsNullOrWhiteSpace(drive.VolumeLabel))
		{
			return $"{drive.VolumeLabel} ({driveName})";
		}

		return driveName;
	}

	private static string BuildRootPath(DriveInfo drive)
	{
		return drive.RootDirectory.FullName;
	}
}