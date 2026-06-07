using MediaDevices;

namespace PlayAroundProject;

#pragma warning disable CA1416 // Validate platform compatibility
internal class Program
{
	static void Main(string[] args)
	{
		Console.WriteLine("Hello World!");

		List<MediaDevice> devices = MediaDevice.GetDevices().ToList();
		foreach (MediaDevice mediaDevice in devices)
		{
			// Check if connected
			Console.WriteLine($"Device.IsConnected    : \"{mediaDevice.IsConnected}\"");

			// Tells if the device is case sensitive (e.g. file system)
			Console.WriteLine($"Device.IsCaseSensitive: \"{mediaDevice.IsCaseSensitive}\"");

			// Readable when not connected
			Console.WriteLine();
			Console.WriteLine("Accessible also without connection");
			Console.WriteLine($"Device.DeviceId       : \"{mediaDevice.DeviceId}\"");
			Console.WriteLine($"Device.Description    : \"{mediaDevice.Description}\"");
			Console.WriteLine($"Device.FriendlyName   : \"{mediaDevice.FriendlyName}\"");
			Console.WriteLine($"Device.Manufacturer   : \"{mediaDevice.Manufacturer}\"");

			using MediaDevice connectedDevice = mediaDevice.ConnectAsReadonly();
			try
			{
				Console.WriteLine();
				Console.WriteLine("Only when Connected");
				Console.WriteLine($"Device.SyncPartner                  : \"{mediaDevice.SyncPartner}\"");
				Console.WriteLine($"Device.FirmwareVersion              : \"{mediaDevice.FirmwareVersion}\"");
				Console.WriteLine($"Device.PowerLevel                   : \"{mediaDevice.PowerLevel}\"");
				Console.WriteLine($"Device.PowerSource                  : \"{mediaDevice.PowerSource}\"");
				Console.WriteLine($"Device.Protocol                     : \"{mediaDevice.Protocol}\"");
				Console.WriteLine($"Device.Model                        : \"{mediaDevice.Model}\"");
				Console.WriteLine($"Device.SerialNumber                 : \"{mediaDevice.SerialNumber}\"");
				Console.WriteLine($"Device.SupportsNonConsumable        : \"{mediaDevice.SupportsNonConsumable}\"");
				Console.WriteLine($"Device.DateTime                     : \"{mediaDevice.DateTime}\"");
				Console.WriteLine(
					$"Device.SupportedFormatsAreOrdered   : \"{mediaDevice.SupportedFormatsAreOrdered}\"");
				Console.WriteLine($"Device.DeviceType                   : \"{mediaDevice.DeviceType}\"");
				Console.WriteLine($"Device.NetworkIdentifier            : \"{mediaDevice.NetworkIdentifier}\"");
				Console.WriteLine($"Device.FunctionalUniqueId           : \"{mediaDevice.FunctionalUniqueId}\"");
				Console.WriteLine($"Device.ModelUniqueId                : \"{mediaDevice.ModelUniqueId}\"");
				Console.WriteLine(
					$"Device.ModelUniqueId                : \"{BitConverter.ToString(mediaDevice.ModelUniqueId!)}\"");
				Console.WriteLine($"Device.Transport                    : \"{mediaDevice.Transport}\"");
				Console.WriteLine($"Device.UseDeviceStage               : \"{mediaDevice.UseDeviceStage}\"");
				Console.WriteLine($"Device.PnPDeviceID                  : \"{mediaDevice.PnPDeviceID}\"");

				MediaDriveInfo[] mediaDrives = connectedDevice.GetDrives();
				foreach (MediaDriveInfo mediaDriveInfo in mediaDrives)
				{
					Console.WriteLine($"MediaDriveInfo.AvailableFreeSpace : {mediaDriveInfo.AvailableFreeSpace}");
					Console.WriteLine($"MediaDriveInfo.DriveFormat        : {mediaDriveInfo.DriveFormat}");
					Console.WriteLine($"MediaDriveInfo.DriveType          : {mediaDriveInfo.DriveType}");
					Console.WriteLine($"MediaDriveInfo.IsReady            : {mediaDriveInfo.IsReady}");
					Console.WriteLine($"MediaDriveInfo.Name               : {mediaDriveInfo.Name}");
					Console.WriteLine($"MediaDriveInfo.RootDirectory      : {mediaDriveInfo.RootDirectory}");
					Console.WriteLine($"MediaDriveInfo.TotalFreeSpace     : {mediaDriveInfo.TotalFreeSpace}");
					Console.WriteLine($"MediaDriveInfo.TotalSize          : {mediaDriveInfo.TotalSize}");
					Console.WriteLine($"MediaDriveInfo.VolumeLabel        : {mediaDriveInfo.VolumeLabel}");
					Console.WriteLine("=========== RootDirectory ===========");
					Console.WriteLine(mediaDriveInfo.RootDirectory);
					MediaDirectoryInfo rootDirectory = mediaDriveInfo.RootDirectory!;
					foreach (MediaFileInfo enumerateFile in rootDirectory.EnumerateFiles())
					{
						Console.WriteLine("File: " + enumerateFile.Name);
					}
					foreach(MediaDirectoryInfo enumerateDirectory in rootDirectory.EnumerateDirectories())
					{
						Console.WriteLine("Directory: " + enumerateDirectory.Name);
						var di = enumerateDirectory.Name;
						break;
					}
					foreach(MediaFileSystemInfo fileSystemInfo in rootDirectory.EnumerateFileSystemInfos())
					{
						Console.WriteLine("FileSystemInfo: " + fileSystemInfo.Name);
						var fsi = fileSystemInfo.Name;
						break;
					}

				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error accessing SyncPartner: {ex.Message}");
			}
			finally
			{
				connectedDevice.Disconnect();
			}


			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (DriveInfo drive in drives)
			{
				bool isReady = drive.IsReady;
				string name = drive.Name;
				DirectoryInfo rootDirectoryInfo = drive.RootDirectory;
				string toStringValue = drive.ToString();

				DriveType driveType = drive.DriveType;

				Console.WriteLine();
				Console.WriteLine("Next Drive IsReady == " + isReady);
				Console.WriteLine("======================");
				if(isReady)
				{
					Console.WriteLine($"IsReady            : {isReady}");
					Console.WriteLine($"Name               : {name}");
					Console.WriteLine($"RootDirectory      : {rootDirectoryInfo}");
					Console.WriteLine($"ToStringValue      : {toStringValue}");
					Console.WriteLine($"DriveType          : {driveType}");

					// Need to be isReady == true
					string driveFormat = drive.DriveFormat;
					long availableFreeSpace = drive.AvailableFreeSpace;
					long totalFreeSpace = drive.TotalFreeSpace;
					long totalSize = drive.TotalSize;
					string volumeLabel = drive.VolumeLabel;
					Console.WriteLine("                       Only ready values");
					Console.WriteLine($"DriveType          : {driveFormat}");
					Console.WriteLine($"AvailableFreeSpace : {availableFreeSpace}");
					Console.WriteLine($"TotalFreeSpace     : {totalFreeSpace}");
					Console.WriteLine($"TotalSize          : {totalSize}");
					Console.WriteLine($"VolumeLabel        : {volumeLabel}");
					//Console.WriteLine($"Drive {name} is ready. Type: {drive.DriveType}, Format: {drive.DriveFormat}");
				}
				else
				{
					Console.WriteLine($"IsReady            : {isReady}");
					Console.WriteLine($"Name               : {name}");
					Console.WriteLine($"RootDirectory      : {rootDirectoryInfo}");
					Console.WriteLine($"ToStringValue      : {toStringValue}");
					Console.WriteLine($"DriveType          : {driveType}");
					//Console.WriteLine($"Drive {name} is not ready. Type: {drive.DriveType}");
				}
			}
		}
	}
}
#pragma warning restore CA1416 // Validate platform compatibility
