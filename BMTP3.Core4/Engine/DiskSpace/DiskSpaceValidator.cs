using Microsoft.Extensions.Logging;

namespace BMTP3.Core4.Engine.DiskSpace;

internal sealed class DiskSpaceValidator : IDiskSpaceValidator
{
	private const long OneMb = 1024 * 1024;
	private const long OneGb = 1024 * OneMb;
	private const long OneHundredMb = 100 * OneMb;
	private const long TenGb = 10 * OneGb;
	private const double CapacityBufferRatio = 1.1;

	private readonly ILogger<DiskSpaceValidator> _logger;

	public DiskSpaceValidator(ILogger<DiskSpaceValidator> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Task EnsureMinimumFreeSpaceAsync(string destinationPath, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		long freeSpace = GetFreeSpace(destinationPath);
		long freeSpaceMb = freeSpace / OneMb;
		long freeSpaceGb = freeSpace / OneGb;

		if(freeSpace < OneHundredMb)
		{
			_logger.LogError(
				"Insufficient disk space on '{Destination}'. Available: {FreeSpaceMb} MB, required minimum: {OneHundredMb} MB.",
				destinationPath, freeSpaceMb, OneHundredMb / OneMb);

			throw new IOException(
				$"Insufficient disk space on destination drive '{Path.GetPathRoot(destinationPath)}'. " +
				$"Available: {freeSpaceMb:N0} MB, required minimum: {OneHundredMb / OneMb} MB.");
		}

		if(freeSpace < OneGb)
		{
			_logger.LogWarning(
				"Low disk space on destination drive '{Drive}': {FreeSpaceMb} MB available. " +
				"Backup may fail if temporary files require additional space.",
				Path.GetPathRoot(destinationPath), freeSpaceMb);
		} else if(freeSpace < TenGb)
		{
			_logger.LogInformation(
				"Disk space on destination drive '{Drive}': {FreeSpaceGb} GB available.",
				Path.GetPathRoot(destinationPath), freeSpaceGb);
		}

		return Task.CompletedTask;
	}

	public Task EnsureSufficientBackupCapacityAsync(string destinationPath, long totalBytesRequired, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if(totalBytesRequired <= 0)
			return Task.CompletedTask;

		long freeSpace = GetFreeSpace(destinationPath);
		long estimatedRequired = (long)(totalBytesRequired * CapacityBufferRatio);

		if(freeSpace < estimatedRequired)
		{
			long freeSpaceMb = freeSpace / OneMb;
			long estimatedMb = estimatedRequired / OneMb;

			_logger.LogError(
				"Insufficient disk space on '{Destination}' for backup content. " +
				"Available: {FreeSpaceMb} MB, estimated required (with overhead): {EstimatedMb} MB.",
				destinationPath, freeSpaceMb, estimatedMb);

			throw new IOException(
				$"Insufficient disk space on destination drive '{Path.GetPathRoot(destinationPath)}'. " +
				$"Available: {freeSpaceMb:N0} MB, estimated required (with overhead): {estimatedMb:N0} MB.");
		}

		return Task.CompletedTask;
	}

	private static long GetFreeSpace(string path)
	{
		string root = Path.GetPathRoot(path) ?? path;
		DriveInfo drive = new(root);
		return drive.AvailableFreeSpace;
	}
}
