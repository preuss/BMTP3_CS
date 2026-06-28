namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Controls how backup items are identified.
///
/// Session — ObjectId  — unique within Connect() only
/// Connection — PUID   — stable across Connect/Disconnect, same USB port
/// Persistent — generated from path + size + dates — stable across USB reconnections
/// </summary>
public enum ItemIdScope
{
	/// <summary>
	/// Uses the WPD ObjectId (<c>file.Id</c>).
	/// Guaranteed unique only within the current <c>Connect()</c> session.
	/// After <c>Disconnect()</c> the device may reassign all ObjectIds.
	///
	/// Usage: Debugging, testing, or one-shot backups without resume.
	/// </summary>
	Session,

	/// <summary>
	/// Uses the WPD PersistentUniqueId / PUID (<c>file.PersistentUniqueId</c>).
	/// Stable across <c>Connect()</c>/<c>Disconnect()</c> cycles within the same
	/// physical USB connection. Most Android devices also maintain PUID
	/// stability across physical USB reconnections, but Apple devices
	/// regenerate PUIDs on every USB replug.
	///
	/// Usage: Same USB port resume, or any scenario where the device stays
	/// on the same port between runs.
	/// </summary>
	Connection,

	/// <summary>
	/// Uses <c>GenerateDeviceUniqueId()</c> — a deterministic identifier
	/// built from file metadata (device path + size + 3 timestamps).
	///
	/// Independent of WPD: no dependency on ObjectId or PUID.
	/// Stable across USB reconnections and port changes
	/// — unlike PUID on Apple devices.
	///
	/// <b>Limitation:</b> because size and timestamps are part of the ID,
	/// any change to file content or dates produces a <i>different</i> ID.
	/// Resume will treat the file as "new" (and the old entry as "removed").
	/// For files that are known to be volatile (caches, live photos, metadata),
	/// consider using <see cref="Connection"/> instead.
	///
	/// This is the default strategy.
	///
	/// Usage: Apple devices, cross-connection resume, stable files
	/// that do not change between backup runs.
	/// </summary>
	Persistent,
}
