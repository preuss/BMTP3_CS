using System;

namespace BMTP3.Core4.Engine.Exceptions;

/// <summary>
/// Thrown by the BackupEngine when a hashing operation for a backup item fails.
/// This wraps the underlying technical exception and provides a domain-level
/// exception the backup program can catch and interpret.
/// </summary>
public sealed class BackupHashException : Exception
{
    /// <summary>
    /// Optional relative path of the item being hashed when the error occurred.
    /// </summary>
    public string? ItemRelativePath { get; }

    public BackupHashException(string message)
        : base(message)
    {
    }

    public BackupHashException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public BackupHashException(string message, string itemRelativePath, Exception innerException)
        : base(message, innerException)
    {
        ItemRelativePath = itemRelativePath;
    }
}
