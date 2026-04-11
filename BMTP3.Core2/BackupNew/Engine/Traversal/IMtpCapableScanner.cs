using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.BackupNew.Engine.Traversal;

/// <summary>
///     Optional extension of <see cref="IBackupScanner" /> for scanners that support MTP devices.
///     <para>
///         <see cref="BackupEngine" /> checks whether <see cref="IBackupScanner" /> also implements
///         this interface when the plan's SourceType is MediaDevice. If so, it calls
///         <see cref="OpenSessionAsync" /> to obtain an <see cref="IMtpDeviceSession" /> that keeps
///         the device connected, and disposes it only after ContentBufferingPipelineStage has
///         finished reading all file content — preventing the race condition where
///         <c>device.Disconnect()</c> runs before <c>OpenRead()</c> is called on staged items.
///     </para>
/// </summary>
public interface IMtpCapableScanner
{
	/// <summary>
	///     Connects to the MTP device identified by <paramref name="plan" /> and returns a session
	///     that must be disposed when all file content has been read.
	/// </summary>
	IMtpDeviceSession OpenSession(BackupPlan plan);
}