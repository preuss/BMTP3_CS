namespace BMTP3.Core2.BackupNew.candidates;
public sealed record Parsed<T>(
	T Value,
	string? Raw
);