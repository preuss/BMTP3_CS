using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Helpers;

namespace BMTP3.Core4.Storage;

internal sealed class BackupFileSystemDriveInfo : IBackupFileSystemDriveInfo
{
	private BackupFileSystemDriveInfo(
		string driveName,
		string displayName,
		string rootPath,
		long totalSize,
		long availableFreeSpace,
		string volumeLabel,
		string driveFormat,
		DriveType driveType
	)
	{
		DriveName = driveName;
		DisplayName = displayName;
		RootPath = rootPath;
		TotalSize = totalSize;
		AvailableFreeSpace = availableFreeSpace;
		VolumeLabel = volumeLabel;
		DriveFormat = driveFormat;
		DriveType = driveType;
	}

	public static BackupFileSystemDriveInfo FromDriveInfo(DriveInfo drive)
	{
		ArgumentNullException.ThrowIfNull(drive);

		if(!drive.IsReady)
		{
			throw new InvalidOperationException($"Drive '{drive.Name}' is not ready.");
		}

		return new BackupFileSystemDriveInfo(
			driveName: BuildDriveName(drive.Name),
			displayName: BuildDisplayName(drive.Name, drive.VolumeLabel),
			rootPath: BuildRootPath(drive.RootDirectory.FullName),
			totalSize: drive.TotalSize,
			availableFreeSpace: drive.AvailableFreeSpace,
			volumeLabel: drive.VolumeLabel,
			driveFormat: drive.DriveFormat,
			driveType: drive.DriveType
		);
	}

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

	private static string BuildDriveName(string sourceDriveName)
	{
		return sourceDriveName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	private static string BuildDisplayName(string sourceDriveName, string sourceVolumeLabel)
	{
		string driveName = BuildDriveName(sourceDriveName);

		if(!string.IsNullOrWhiteSpace(sourceVolumeLabel))
		{
			return $"{sourceVolumeLabel} ({driveName})";
		}

		return driveName;
	}

	private static string BuildRootPath(string sourceRootDirectoryFullName)
	{
		return PathHelper.ToInternalCanonicalUri(sourceRootDirectoryFullName, BackupSourceType.FileSystem);
	}
}