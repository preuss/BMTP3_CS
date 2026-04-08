using BMTP3.Core2.BackupNew.candidates;

namespace BMTP3.Core2.BackupNew.exifreader.definitions;

/// <summary>
///     Definition specifically for XMP which uses Strings (Namespace + Property Name).
/// </summary>
public record XmpTagDefinition(
	TimestampRole Role,
	string Namespace,
	string PropertyName
);