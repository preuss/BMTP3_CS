namespace BMTP3.Core4.Api.Models.Enums;

/// <summary>
/// Controls how backup items are identified.
///
/// Session — ObjectId  — unique within Connect() only
/// Connection — PUID   — stable across Connect/Disconnect, device-dependent across USB replug
/// Persistent — generated — independent of WPD identifiers
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
	/// physical USB connection.
	///
	/// Most devices (Windows, cameras, Android) also maintain PUID
	/// stability across physical USB reconnections.
	/// Apple devices (iPhone, iPad) regenerate PUIDs on every USB replug.
	/// </summary>
	Connection,

	/// <summary>
	/// Uses <c>GenerateDeviceUniqueId()</c> — a deterministic identifier
	/// built from stable file metadata (device path + size + 3 timestamps).
	///
	/// The ID is independent of WPD: no dependency on ObjectId or PUID.
	/// It is computed purely from the file's own properties, making it
	/// immune to WPD identifier instability across sessions or connections.
	///
	/// Same file always produces the same ID regardless of session state,
	/// connection state, or device firmware behaviour.
	///
	/// This is the default strategy.
	///
	/// Usage: Apple devices, cross-connection resume, or any scenario
	/// where WPD identifiers cannot be trusted.
	/// </summary>
	Persistent,
}
