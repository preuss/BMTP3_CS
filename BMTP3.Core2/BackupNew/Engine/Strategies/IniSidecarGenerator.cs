using System.Text;
using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Domain.Item;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
///     Simple INI-style sidecar generator. Produces a flat key=value listing.
/// </summary>
public class IniSidecarGenerator : ISidecarGenerator
{
	private readonly ILogger<IniSidecarGenerator> _logger;

	public IniSidecarGenerator(ILogger<IniSidecarGenerator> logger)
	{
		_logger = logger;
	}

	public Task<bool> GenerateAsync(IBackupItem item, CancellationToken ct)
	{
		try
		{
			StringBuilder sb = new();
			IDictionary<string, object?> dict = item.Metadata.ToDictionary();
			foreach (KeyValuePair<string, object?> kv in dict)
			{
				if (string.Equals(kv.Key, "hashes", StringComparison.OrdinalIgnoreCase) &&
				    kv.Value is Dictionary<HashType, string> hashes)
				{
					foreach (KeyValuePair<HashType, string> hash in hashes)
					{
						sb.AppendLine($"{hash.Key}={hash.Value}");
					}

					continue;
				}

				string value = kv.Value?.ToString() ?? string.Empty;
				sb.AppendLine($"{kv.Key}={value}");
			}

			string? final = item.Metadata.Get<string>(MetadataKey.FinalTargetPath);
			string sidecarPath;
			if (!string.IsNullOrWhiteSpace(final))
			{
				sidecarPath = Path.ChangeExtension(final, ".ini");
			}
			else
			{
				string? sourceName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? Guid.NewGuid().ToString();
				sidecarPath = Path.ChangeExtension(sourceName, ".ini");
			}

			string? dir = Path.GetDirectoryName(sidecarPath);
			if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			File.WriteAllText(sidecarPath, sb.ToString());
			try
			{
				item.Metadata.Set(MetadataKey.SidecarPath, sidecarPath);
			}
			catch
			{
			}

			item.AddLog($"Wrote sidecar: {sidecarPath}", "Sidecar");
			_logger.LogDebug("Generated INI sidecar for item at {SidecarPath}", sidecarPath);
			return Task.FromResult(true);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to generate INI sidecar");
			item.AddLog($"Sidecar generation failed: {ex.Message}", "Sidecar");
			return Task.FromResult(false);
		}
	}
}