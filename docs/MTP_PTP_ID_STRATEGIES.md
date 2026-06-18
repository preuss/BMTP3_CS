# MTP/PTP — Object Identity Strategies

---

## Core Facts

ObjectId   = valid within current session only  
PUID       = persistent across sessions, but not guaranteed across physical USB reconnects  
Connection = device physically connected (USB insert)  
Session    = Connect() / Disconnect() cycle within a connection  

---

## Connection vs Session

**Connection** = The device is physically connected (USB cable inserted, driver loaded)

**Session** = `Connect()` opens an active communication context. `Disconnect()` ends it.

A single connection can contain many sessions.

A new connection (unplug/replug) may cause the device to reassign ObjectIds and, on some devices, PUIDs.

---

## The Three Strategies

| Navn         | Scope         | Kilde                           | In session | Between sessions | Between connections |
|--------------|---------------|---------------------------------|:----------:|:----------------:|:-------------------:|
| `session`    | Session       | `file.Id`                       | ✅         | ❌              | ❌                  |
| `connection` | Connection    | `file.PersistentUniqueId`       | ✅         | ✅              | ❓ (Apple: ❌)      |
| `persistent` | DeviceUnique  | `GenerateDeviceUniqueId()`      | ✅         | ✅              | ✅                  |

`session` — session-scoped identity only.

`connection` — default choice; connection stability depends on device.

`persistent` — generated from stable file metadata; independent of WPD ObjectId/PUID behavior.

---

## Device PUID Behavior

| Device type                 | Between sessions | Between connections |
|-----------------------------|:----------------:|:-------------------:|
| Apple (iPhone, iPad)        | ✅               | ❌                  |
| Android / other             | ✅               | ❓                  |
| `GenerateDeviceUniqueId()`  | ✅               | ✅                  |

---

## `GenerateDeviceUniqueId()`

Generates a deterministic identity from stable file metadata.

```csharp
internal static string GenerateDeviceUniqueId(
    string fullFilePath,
    ulong size,
    DateTimeOffset? dateCreated,
    DateTimeOffset? dateModified,
    DateTimeOffset? dateAuthored
)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(fullFilePath);

    return $"{fullFilePath}_{size}_{dateCreated?.UtcTicks}_{dateModified?.UtcTicks}_{dateAuthored?.UtcTicks}";
}
```

Format:

```
fullFilePath_size_dateCreatedUtcTicks_dateModifiedUtcTicks_dateAuthoredUtcTicks
```

Example:

```
/Internal Storage/DCIM/IMG_001.jpg_3456789_638510112000000000_638510115000000000_638510115000000000
```

---

## Summary

| Identity      | Best for                                              | Limitation                                                    |
|---------------|-------------------------------------------------------|---------------------------------------------------------------|
| `session`     | Current active session                                | Not usable after `Disconnect()`                               |
| `connection`  | Resume across `Connect()` / `Disconnect()`            | Device-dependent after USB unplug/replug                      |
| `persistent`  | Resume across sessions and USB reconnects             | Generated from metadata, not provided by WPD                  |

---

## When to Use

Use `session` for debugging or one-shot backups within a single session.

Use `connection` as the default for most devices.

Use `persistent` for Apple devices, or any device where PUID cannot be trusted across USB reconnects.
