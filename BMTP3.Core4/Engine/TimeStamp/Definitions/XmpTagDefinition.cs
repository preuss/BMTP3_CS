using BMTP3.Core4.Engine.TimeStamp.Candidates;

namespace BMTP3.Core4.Engine.TimeStamp.Definitions;

/// <summary>
///     Definition specifically for XMP which uses Strings (Namespace + Property Name).
/// </summary>
public record XmpTagDefinition(
	TimestampRole Role,
	string Namespace,
	string PropertyName
);