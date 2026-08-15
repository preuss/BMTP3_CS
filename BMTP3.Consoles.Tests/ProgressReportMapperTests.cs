using BMTP3.Consoles.Progress;
using BMTP3.Core4.Api.Models;
using BMTP3.Core4.Api.Models.Enums;
using Xunit;

namespace BMTP3.Consoles.Tests;

public class ProgressReportMapperTests
{
	private static BackupProgress Progress(BackupProgressPhase phase)
	{
		return new BackupProgress
		{
			CurrentPhase = phase,
			DirectoriesTraversed = 5,
			FilesDiscovered = 120,
			TotalFilesSelected = 100,
			FilesSucceeded = 40,
			FilesSkipped = 10,
			FilesFailed = 2,
			BytesProcessed = 5000,
			TotalBytesSelected = 100_000
		};
	}

	private static BackupProgressItem Item(BackupProgressItemPhase phase, long length, long? bytesProcessed, string path = @"DCIM\IMG_001.jpg")
	{
		return new BackupProgressItem
		{
			RelativeFilePath = path,
			Length = length,
			BytesProcessed = bytesProcessed,
			Phase = phase
		};
	}

	[Fact]
	public void ToReport_Scanning_SetsZeroProgressAndDiscoveryText()
	{
		ProgressReport report = ProgressReportMapper.ToReport(Progress(BackupProgressPhase.Scanning));

		Assert.Equal(0, report.FilesCompleted);
		Assert.Equal(1, report.FilesTotal);
		Assert.Equal("5 dirs, 120 files", report.OverallDisplayText);
		Assert.Equal(BackupProgressPhase.Scanning, report.OverallPhase);
		Assert.Null(report.ActiveFileName);
		Assert.Equal(5, report.DirectoriesTraversed);
		Assert.Equal(120, report.FilesDiscovered);
	}

	[Fact]
	public void ToReport_FilePhaseWithActiveFile_MapsCountsAndActiveFile()
	{
		BackupProgress p = Progress(BackupProgressPhase.Transferring) with
		{
			ActiveFiles = new[] { Item(BackupProgressItemPhase.Transferring, 1000, 400) }
		};

		ProgressReport report = ProgressReportMapper.ToReport(p);

		Assert.Equal(52, report.FilesCompleted);
		Assert.Equal(100, report.FilesTotal);
		Assert.Equal(@"DCIM\IMG_001.jpg (52/100 files)", report.OverallDisplayText);
		Assert.Equal(@"DCIM\IMG_001.jpg", report.ActiveFileName);
		Assert.Equal(BackupProgressItemPhase.Transferring, report.ActiveFilePhase);
		Assert.Equal(400, report.ActiveFileBytesRead);
		Assert.Equal(1000, report.ActiveFileBytesTotal);
		Assert.Equal(5000, report.TotalBytesProcessed);
		Assert.Equal(100_000, report.TotalBytesSelected);
	}

	[Fact]
	public void ToReport_FilePhaseWithoutActiveFile_ShowsCountsOnly()
	{
		ProgressReport report = ProgressReportMapper.ToReport(Progress(BackupProgressPhase.Transferring));

		Assert.Equal(52, report.FilesCompleted);
		Assert.Equal(100, report.FilesTotal);
		Assert.Equal("52/100 files", report.OverallDisplayText);
		Assert.Null(report.ActiveFileName);
	}

	[Fact]
	public void ToReport_ZeroSelectedFiles_MinTotalToOne()
	{
		BackupProgress p = Progress(BackupProgressPhase.Transferring) with { TotalFilesSelected = 0 };

		ProgressReport report = ProgressReportMapper.ToReport(p);

		Assert.Equal(1, report.FilesTotal);
	}

	[Fact]
	public void ToReport_NullBytesProcessed_ReadsZero()
	{
		BackupProgress p = Progress(BackupProgressPhase.Transferring) with
		{
			ActiveFiles = new[] { Item(BackupProgressItemPhase.Finalizing, 100, null) }
		};

		ProgressReport report = ProgressReportMapper.ToReport(p);

		Assert.Equal(0, report.ActiveFileBytesRead);
		Assert.Equal(100, report.ActiveFileBytesTotal);
	}

	[Fact]
	public void ToReport_Null_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => ProgressReportMapper.ToReport(null!));
	}
}