using Microsoft.Extensions.Logging;
using System.Text;

namespace BMTP3.Core3.Sidecar;

/// <summary>
/// Simple sidecar generator that creates .sidecar files in INI-like format.
/// Contains metadata and all computed hash values for backup verification.
/// </summary>
public class SimpleSidecarGenerator : ISidecarGenerator
{
	private readonly ILogger<SimpleSidecarGenerator> _logger;

	public SimpleSidecarGenerator(ILogger<SimpleSidecarGenerator> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task GenerateAsync(BackupItem item, string destinationDirectory, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(destinationDirectory);
		ct.ThrowIfCancellationRequested();

		if (!Directory.Exists(destinationDirectory))
		{
			throw new DirectoryNotFoundException($"Destination directory not found: {destinationDirectory}");
		}

		var sidecarPath = Path.Combine(destinationDirectory, $"{item.Name}.sidecar");

		_logger.LogDebug("Generating sidecar for {ItemName} → {SidecarPath}", item.Name, sidecarPath);

		try
		{
			var sb = new StringBuilder();

			// Metadata section
			sb.AppendLine("[Metadata]");
			sb.AppendLine($"FileName={item.Name}");
			sb.AppendLine($"SourcePath={item.SourcePath}");
			sb.AppendLine($"FileSize={item.SizeInBytes}");
			sb.AppendLine($"CreatedAt={item.CreatedAt:O}");
			sb.AppendLine($"ModifiedAt={item.ModifiedAt:O}");
			sb.AppendLine($"TransferredAt={DateTime.UtcNow:O}");
			sb.AppendLine();

			// Hashes section (if hashes computed)
			if (item.Hashes?.Count > 0)
			{
				sb.AppendLine("[Hashes]");
				foreach (var kvp in item.Hashes.OrderBy(x => x.Key.ToString()))
				{
					sb.AppendLine($"{kvp.Key}={kvp.Value}");
				}
				sb.AppendLine();
			}

			// Metadata section (if extracted)
			if (item.Metadata?.Count > 0)
			{
				sb.AppendLine("[Attributes]");
				foreach (var kvp in item.Metadata.OrderBy(x => x.Key))
				{
					sb.AppendLine($"{kvp.Key}={kvp.Value}");
				}
			}

			// Write file
			await File.WriteAllTextAsync(sidecarPath, sb.ToString(), Encoding.UTF8, ct).ConfigureAwait(false);

			_logger.LogDebug("Sidecar generated: {SidecarPath}", sidecarPath);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate sidecar for {ItemName}", item.Name);
			throw;
		}
	}
}
