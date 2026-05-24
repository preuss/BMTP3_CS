namespace BMTP3.Core4.Engine.TimeStamp.Candidates;

public sealed record Parsed<T>(
	T Value,
	string? Raw
);