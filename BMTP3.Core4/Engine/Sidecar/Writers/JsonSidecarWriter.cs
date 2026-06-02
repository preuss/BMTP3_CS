using System.Text;
using System.Text.Json;
using BMTP3.Core4.Engine.Sidecar.Document;

namespace BMTP3.Core4.Engine.Sidecar.Writers;

internal sealed class JsonSidecarWriter : ISidecarWriter
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
	};

	public async Task WriteToFileAsync(SidecarDocument document, string filePath, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

		string content = WriteToString(document);
		await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, cancellationToken);
	}

	public static string WriteToString(SidecarDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);

		Dictionary<string, object?> root = new(StringComparer.OrdinalIgnoreCase);

		foreach(SidecarSection section in document.GetSortedSections())
		{
			Dictionary<string, string?> sectionDict = new(StringComparer.OrdinalIgnoreCase);

			foreach(SidecarProperty property in section.GetSortedProperties())
			{
				sectionDict[property.Key] = property.Value;
			}

			root[section.Name] = sectionDict;
		}

		return JsonSerializer.Serialize(root, JsonOptions);
	}
}
