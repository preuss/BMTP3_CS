using System.Text;
using BMTP3.Core4.Engine.Sidecar.Document;

namespace BMTP3.Core4.Engine.Sidecar.Writers;

internal sealed class IniSidecarWriter : ISidecarWriter
{
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

		StringBuilder sb = new();

		if(document.HeaderComment is { Count: > 0 })
		{
			foreach(string headerLine in document.HeaderComment)
			{
				if(headerLine.Contains('\n'))
				{
					foreach(string line in headerLine.Split('\n', StringSplitOptions.RemoveEmptyEntries))
					{
						sb.AppendLine($"# {line.TrimEnd('\r')}");
					}
				}
				else
				{
					sb.AppendLine($"# {headerLine}");
				}
			}

			sb.AppendLine();
		}

		foreach(SidecarSection section in document.GetSortedSections())
		{
			if(section.Comment is not null)
			{
				foreach(string commentLine in section.Comment.Split('\n', StringSplitOptions.RemoveEmptyEntries))
				{
					sb.AppendLine($"# {commentLine.TrimEnd('\r')}");
				}
			}

			sb.AppendLine($"[{section.Name}]");

			foreach(SidecarProperty property in section.GetSortedProperties())
			{
				if(property.Comment is not null)
				{
					foreach(string commentLine in property.Comment.Split('\n', StringSplitOptions.RemoveEmptyEntries))
					{
						sb.AppendLine($"# {commentLine.TrimEnd('\r')}");
					}
				}

				sb.AppendLine($"{property.Key}={property.Value}");
			}

			sb.AppendLine();
		}

		return sb.ToString();
	}
}
