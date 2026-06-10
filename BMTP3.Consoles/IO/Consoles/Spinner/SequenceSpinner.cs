using Spectre.Console;
using System.Collections.Immutable;

namespace BMTP3.Consoles.IO.Consoles.Spinner;

public sealed class SequenceSpinner : Spectre.Console.Spinner
{
	public const string Sequence1 = @"/-\|";
	public const string Sequence3 = @".o0o";
	public const string Sequence2 = @"<^>v";
	public const string Sequence4 = @"#■.";
	public const string Sequence5 = @"▄▀";
	public const string Sequence6 = @"└┘┐┌";
	public const string Sequence7 = @".-|+oOØ";

	private readonly IReadOnlyList<string> frames;
	public SequenceSpinner(string sequence)
	{
		frames = sequence.Select(c => c.ToString()).ToImmutableList();
	}

	// The interval for each frame
	public override TimeSpan Interval => TimeSpan.FromMilliseconds(200);

	// Whether or not the spinner contains unicode characters
	public override bool IsUnicode => false;

	// The individual frames of the spinner
	public override IReadOnlyList<string> Frames => frames;
}
