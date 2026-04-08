using System.Text.Json;
using BMTP3.Core2.BackupNew.Api.Request.Enums;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

/// <summary>
///     Utility to read hashes from common sidecar formats (JSON with hashes object or simple key=value files).
/// </summary>
public static class SidecarReader
{
	public static async Task<string?> TryReadHashFromSidecarAsync(string destPath, HashType algorithm,
		CancellationToken ct)
	{
		string[] candidates = new[]
		{
			destPath + ".bmtp3.json",
			destPath + ".meta.json",
			destPath + ".metadata.json",
			destPath + ".json",
			Path.ChangeExtension(destPath, ".meta"),
			Path.ChangeExtension(destPath, ".ini")
		};

		foreach (string sidecar in candidates)
		{
			if (!File.Exists(sidecar))
			{
				continue;
			}

			ct.ThrowIfCancellationRequested();

			try
			{
				string text = await File.ReadAllTextAsync(sidecar, ct).ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}

				using JsonDocument doc = JsonDocument.Parse(text);
				JsonElement root = doc.RootElement;

				if (root.ValueKind == JsonValueKind.Object)
				{
					// try hashes object (Case-Insensitive search)
					JsonElement hashesEl = default;
					bool foundHashes = false;
					foreach (JsonProperty prop in root.EnumerateObject())
					{
						if (string.Equals(prop.Name, "hashes", StringComparison.OrdinalIgnoreCase))
						{
							hashesEl = prop.Value;
							foundHashes = true;
							break;
						}
					}

					if (foundHashes && hashesEl.ValueKind == JsonValueKind.Object)
					{
						// try algorithm name key (Case-Insensitive)
						foreach (JsonProperty prop in hashesEl.EnumerateObject())
						{
							if (string.Equals(prop.Name, algorithm.ToString(), StringComparison.OrdinalIgnoreCase))
							{
								return prop.Value.GetString();
							}
						}

						// legacy SHA256 key for SHA2_256
						if (algorithm == HashType.SHA2_256)
						{
							foreach (JsonProperty prop in hashesEl.EnumerateObject())
							{
								if (string.Equals(prop.Name, "SHA256", StringComparison.OrdinalIgnoreCase))
								{
									return prop.Value.GetString();
								}
							}
						}
					}

					// direct key fallback (Case-Insensitive)
					foreach (JsonProperty prop in root.EnumerateObject())
					{
						if (string.Equals(prop.Name, algorithm.ToString(), StringComparison.OrdinalIgnoreCase))
						{
							return prop.Value.GetString();
						}

						if (algorithm == HashType.SHA2_256 &&
						    string.Equals(prop.Name, "SHA256", StringComparison.OrdinalIgnoreCase))
						{
							return prop.Value.GetString();
						}
					}
				}
			}
			catch
			{
				/* ignore and try next */
			}

			// try simple key=value lines
			try
			{
				foreach (string line in File.ReadLines(sidecar))
				{
					ct.ThrowIfCancellationRequested();
					int idx = line.IndexOf('=');
					if (idx <= 0)
					{
						continue;
					}

					string key = line.Substring(0, idx).Trim();
					string val = line.Substring(idx + 1).Trim();
					if (string.Equals(key, algorithm.ToString(), StringComparison.OrdinalIgnoreCase) ||
					    (algorithm == HashType.SHA2_256 &&
					     string.Equals(key, "SHA256", StringComparison.OrdinalIgnoreCase)))
					{
						if (!string.IsNullOrWhiteSpace(val))
						{
							return val;
						}
					}
				}
			}
			catch
			{
				/* ignore */
			}
		}

		return null;
	}
}