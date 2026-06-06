using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Storage;

internal interface IBackupDriveInfo
{
	/// <summary>
	/// Unique identifier used internally by the backup model.
	/// For file system this is typically the same as <see cref="DriveName"/> (e.g. "C:").
	/// For MTP this is "{FriendlyName}/{DriveName}" (e.g. "Apple iPad/Internal Storage").
	/// </summary>
	string Id { get; }

	/// <summary>
	/// Unique drive name within a backup source type.
	/// For file system this is the drive name (e.g. "C:").
	/// For MTP this is the media drive name (e.g. "Internal Storage").
	/// </summary>
	string DriveName { get; }

	/// <summary>
	/// For mtp DisplayName is $"{device.FriendlyName}"
	///     E.g. "My Phone"
	/// For file system DisplayName is $"{drive.VolumeLabel} ({drive.Name})" if VolumeLabel is not empty, otherwise drive.Name
	///     E.g. "System (C:)" or "C:"
	/// </summary>
	string DisplayName { get; }

	BackupSourceType SourceType { get; }

	/// <summary>
	/// For mtp RootPath is $"mtp://{device.FriendlyName}/{DriveName}"
	///     E.g. "mtp://My Phone/Internal Storage"
	/// For file system RootPath is drive.Name (e.g. "C:\")
	///     E.g. "C:\"
	/// </summary>
	string RootPath { get; }

	long TotalSize { get; }

	long AvailableFreeSpace { get; }
}
