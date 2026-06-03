using BMTP3.Core4.Engine.Sidecar.Document;
using System.Text.Json;

namespace BMTP3.Core4.Engine.Sidecar.Writers;

internal sealed class JsonSidecarWriter : ISidecarWriter
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
	};

	public Task WriteToStreamAsync(SidecarDocument document, Stream stream, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentNullException.ThrowIfNull(stream);

		cancellationToken.ThrowIfCancellationRequested();

		Dictionary<string, Dictionary<string, string?>> root = CreateSerializableModel(document);
		return JsonSerializer.SerializeAsync(stream, root, JsonOptions, cancellationToken);
	}

	public static string WriteToString(SidecarDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);

		Dictionary<string, Dictionary<string, string?>> root = CreateSerializableModel(document);
		return JsonSerializer.Serialize(root, JsonOptions);
	}

	private static Dictionary<string, Dictionary<string, string?>> CreateSerializableModel(SidecarDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);

		Dictionary<string, Dictionary<string, string?>> root = new(StringComparer.OrdinalIgnoreCase);

		foreach (SidecarSection section in document.GetSortedSections())
		{
			Dictionary<string, string?> sectionDict = new(StringComparer.OrdinalIgnoreCase);

			foreach (SidecarProperty property in section.GetSortedProperties())
			{
				sectionDict[property.Key] = property.Value;
			}

			root[section.Name] = sectionDict;
		}

		return root;
	}
}