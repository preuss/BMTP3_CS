using BMTP3.Consoles.IO.Consoles.Progress.Columns;
using BMTP3.Core4.Api.Models.Enums;
using Xunit;

namespace BMTP3.Consoles.Tests;

public class PhaseColumnTests
{
	[Theory]
	[InlineData(BackupProgressPhase.Starting, "Start")]
	[InlineData(BackupProgressPhase.Scanning, "Scan")]
	[InlineData(BackupProgressPhase.Transferring, "Transfer")]
	[InlineData(BackupProgressPhase.Completed, "Done")]
	public void ToShortLabel_OverallPhase(BackupProgressPhase phase, string expected)
	{
		Assert.Equal(expected, PhaseColumn.ToShortLabel(phase));
	}

	[Theory]
	[InlineData(BackupProgressItemPhase.Transferring, "Transfer")]
	[InlineData(BackupProgressItemPhase.ProcessingMetadata, "Meta")]
	[InlineData(BackupProgressItemPhase.Hashing, "Hash")]
	[InlineData(BackupProgressItemPhase.Finalizing, "Final")]
	public void ToShortLabel_ItemPhase(BackupProgressItemPhase phase, string expected)
	{
		Assert.Equal(expected, PhaseColumn.ToShortLabel(phase));
	}

	[Theory]
	[InlineData(0, true, "Transfer")]   // Transferring
	[InlineData(1, true, "Meta")]       // ProcessingMetadata
	[InlineData(2, true, "Hash")]       // Hashing
	[InlineData(3, true, "Final")]      // Finalizing
	[InlineData(-1, true, "")]
	[InlineData(0, false, "Start")]     // Starting
	[InlineData(1, false, "Scan")]      // Scanning
	[InlineData(2, false, "Transfer")]  // Transferring
	[InlineData(3, false, "Done")]      // Completed
	public void GetPhaseText_MapsRawValues(int raw, bool isByteTask, string expected)
	{
		Assert.Equal(expected, PhaseColumn.GetPhaseText(raw, isByteTask));
	}

	[Fact]
	public void ToShortLabel_UnknownOverallPhase_FallsBackToName()
	{
		Assert.Equal("99", PhaseColumn.ToShortLabel((BackupProgressPhase)99));
	}

	[Fact]
	public void ToShortLabel_UnknownItemPhase_FallsBackToName()
	{
		Assert.Equal("99", PhaseColumn.ToShortLabel((BackupProgressItemPhase)99));
	}
}