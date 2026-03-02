using BMTP3.Core.Exceptions;
using MediaDevices;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZLogger;

namespace BMTP3.Core.Handlers {
	public class BackupExceptionHandlerService {
		private static readonly ILogger<BackupExceptionHandlerService> logger = LogManager.GetLogger<BackupExceptionHandlerService>();

		public void HandleCOMException(COMException e, string deviceFriendlyName) {
			if(e.Message.Contains("(0x800710D2)")) {
				logger.ZLogError($"The library, drive, or media pool is empty. (0x800710D2)");
				throw new ComBackupException($"The Device '{deviceFriendlyName}' exists but is empty. Please open and activate the physical device.", e);
			} else if(e.Message.Contains("(0x8007001E)")) {
				logger.ZLogError($"The device is not ready. (0x8007001E)");
				throw new ComBackupException($"The Device '{deviceFriendlyName}' is not ready. Please wait and try again.", e);
			} else {
				logger.ZLogError($"COMException occurred: {e.Message}");
				//throw new ComBackupException($"COMException occurred: {e.Message}", e);
			}
		}

		[SupportedOSPlatform("windows10.0")]
		public void HandleCOMException(COMException e, MediaDevice mediaDevice) {
			HandleCOMException(e, mediaDevice.FriendlyName);
		}
	}
}
