namespace BMTP3.Core4.Engine.Sidecar.Writers;

internal sealed class IniSidecarWriterOptions
{
	public bool WriteComments { get; init; } = false;

	public bool PreserveEmptyCommentLines { get; init; } = true;

	public bool WriteKeysWithNullValues { get; init; } = true;
}
