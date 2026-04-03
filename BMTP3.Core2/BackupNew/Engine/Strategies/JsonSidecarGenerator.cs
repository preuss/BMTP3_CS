using BMTP3.Core2.BackupNew.Domain.Item;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
/// Skeleton implementation of a JSON sidecar generator.
/// Currently just a placeholder.
/// </summary>
	public class JsonSidecarGenerator : ISidecarGenerator
	{
		private readonly ILogger<JsonSidecarGenerator> _logger;

		public JsonSidecarGenerator(ILogger<JsonSidecarGenerator> logger)
		{
			_logger = logger;
		}

		public Task<bool> GenerateAsync(IBackupItem item, CancellationToken ct)
		{
            try
            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

                // Extract a dictionary representation of metadata for serialization
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                var sourceDict = item.Metadata.ToDictionary();
                foreach(var kv in sourceDict)
                {
                    dict[kv.Key] = kv.Value;
                }

                string json = JsonSerializer.Serialize(dict, jsonOptions);

                // Determine output path. Prefer FinalTargetPath directory if available; otherwise use current directory.
                string? final = item.Metadata.Get<string>(MetadataKey.FinalTargetPath);
                string sidecarPath;
                if(!string.IsNullOrWhiteSpace(final))
                {
                    sidecarPath = final + ".bmtp3.json";
                }
                else
                {
                    string? sourceName = item.Metadata.Get<string>(MetadataKey.SourceFileName) ?? Guid.NewGuid().ToString();
                    sidecarPath = sourceName + ".bmtp3.json";
                }

                // Ensure directory exists
                string? dir = Path.GetDirectoryName(sidecarPath);
                if(!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.WriteAllText(sidecarPath, json);
                // Record written path into metadata for consumers/tests
                try { item.Metadata.Set(MetadataKey.SidecarPath, sidecarPath); } catch { }
                item.AddLog($"Wrote sidecar: {sidecarPath}", "Sidecar");
                _logger.LogDebug("Generated sidecar for item at {SidecarPath}", sidecarPath);
                return Task.FromResult(true);
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate JSON sidecar");
                item.AddLog($"Sidecar generation failed: {ex.Message}", "Sidecar");
                return Task.FromResult(false);
            }
		}
	}
