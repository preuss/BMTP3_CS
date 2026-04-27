using BMTP3.Core2.BackupNew.Api.Progress.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using System.Text;
using System.Text.Json;
using BMTP3.Core2.BackupNew.Engine.Strategies;

namespace BMTP3.Core2.BackupNew.Engine.Steps.SidecarGenerationStep;

/// <summary>
///     Generates sidecar files for items using the configured <see cref="ISidecarGenerator" />.
/// </summary>
public class SidecarGenerationItemStep : IBackupItemStep<BackupPlan, bool>
{
	private readonly ISidecarGeneratorFactory _factory;

	/// <summary>
	///     Initializes a new instance of <see cref="SidecarGenerationItemStep" />.
	///     The step will resolve the appropriate ISidecarGenerator implementation from the provided factory based on the
	///     BackupPlan.SidecarFormat.
	/// </summary>
	public SidecarGenerationItemStep(BackupPlan context, ISidecarGeneratorFactory factory)
	{
		Context = context ?? throw new ArgumentNullException(nameof(context));
		_factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

   public string Name => "Sidecar Generation";
   public FilePhase Phase => FilePhase.None; // No direct match in new model
	public BackupPlan Context { get; }

	/// <summary>
	///     Executes sidecar generation for a single item.
	/// </summary>
	/// <param name="item">The backup item.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>True if a sidecar was generated; otherwise false.</returns>
	public async Task<bool> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);

		try
		{
           // DryRun: generate preview metadata but do not write files
			if (Context.DryRun)
			{
				try
				{
					string previewContent;
					string previewPath;
					switch (Context.SidecarFormat)
					{
						case Api.Request.Enums.SidecarFormat.Ini:
						{
							StringBuilder sb = new();
							IDictionary<string, object?> dict = item.Metadata.ToDictionary();
							foreach (KeyValuePair<string, object?> kv in dict)
							{
                                if (string.Equals(kv.Key, "hashes", StringComparison.OrdinalIgnoreCase) && kv.Value is System.Collections.IDictionary hashDict)
								{
									foreach (System.Collections.DictionaryEntry hash in hashDict)
									{
										sb.AppendLine($"{hash.Key}={hash.Value}");
									}
									continue;
								}
								string value = kv.Value?.ToString() ?? string.Empty;
								sb.AppendLine($"{kv.Key}={value}");
							}
							previewContent = sb.ToString();
							string? final = item.Metadata.Get<string>(Domain.Item.MetadataKey.FinalTargetPath);
							if (!string.IsNullOrWhiteSpace(final))
							{
								previewPath = Path.ChangeExtension(final, ".ini");
							}
							else
							{
								string? sourceName = item.Metadata.Get<string>(Domain.Item.MetadataKey.SourceFileName) ?? Guid.NewGuid().ToString();
								previewPath = Path.ChangeExtension(sourceName, ".ini");
							}
							break;
						}
						case Api.Request.Enums.SidecarFormat.Json:
						{
							JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
							IDictionary<string, object?> dict = item.Metadata.ToDictionary();
							previewContent = JsonSerializer.Serialize(dict, jsonOptions);
							string? final = item.Metadata.Get<string>(Domain.Item.MetadataKey.FinalTargetPath);
							if (!string.IsNullOrWhiteSpace(final))
							{
								previewPath = final + ".bmtp3.json";
							}
							else
							{
								string? sourceName = item.Metadata.Get<string>(Domain.Item.MetadataKey.SourceFileName) ?? Guid.NewGuid().ToString();
								previewPath = sourceName + ".bmtp3.json";
							}
							break;
						}
						default:
							// Unknown format – produce a simple key=value listing as fallback
							StringBuilder sb2 = new();
							foreach (KeyValuePair<string, object?> kv in item.Metadata.ToDictionary())
							{
								sb2.AppendLine($"{kv.Key}={kv.Value}");
							}
							previewContent = sb2.ToString();
							previewPath = (item.Metadata.Get<string>(Domain.Item.MetadataKey.SourceFileName) ?? "sidecar") + ".preview";
							break;
					}

					// Store preview in metadata for tests/preview UI – do not write to disk
					item.Metadata.Set(Domain.Item.MetadataKey.SidecarPreviewContent, previewContent);
					item.Metadata.Set(Domain.Item.MetadataKey.SidecarPathPreview, previewPath);
					item.AddLog($"Generated sidecar preview for {previewPath}", Name);
					return true;
				}
				catch (Exception ex)
				{
					item.AddLog($"Sidecar preview generation failed: {ex.Message}", Name);
					return false;
				}
			}

			// Resolve generator based on configured sidecar format in the plan
			ISidecarGenerator? generator = _factory.Create(Context.SidecarFormat);
			if (generator == null)
			{
				item.AddLog("No sidecar generator available.", Name);
				return true; // Not an error - sidecar is optional
			}

			bool generated = await generator.GenerateAsync(item, ct);

			if (generated)
			{
				item.AddLog("Sidecar generated.", Name);
			}
			else
			{
				// Sidecar is critical - fail the item when generation fails
				item.Fail("Sidecar generation failed: generator returned false", Name);
			}

			return generated;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			// Sidecar is critical - fail the item when generation throws
			item.Fail($"Sidecar generation failed: {ex.Message}", Name);
			return false;
		}
	}
}