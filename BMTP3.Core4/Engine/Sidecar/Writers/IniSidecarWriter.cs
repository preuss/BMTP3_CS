using BMTP3.Core4.Engine.Sidecar.Document;
using System.Text;

namespace BMTP3.Core4.Engine.Sidecar.Writers;

internal sealed class IniSidecarWriter : ISidecarWriter
{
	// Use deterministic CRLF line endings for INI output.
	private const string NewlineCrlf = "\r\n";

	private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private readonly IniSidecarWriterOptions options;

	public IniSidecarWriter() : this(new IniSidecarWriterOptions())
	{
	}
	public IniSidecarWriter(IniSidecarWriterOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		this.options = options;
	}

	public async Task WriteToStreamAsync(SidecarDocument document, Stream stream, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentNullException.ThrowIfNull(stream);

		cancellationToken.ThrowIfCancellationRequested();

		await using StreamWriter writer = new(stream, Utf8NoBom, bufferSize: 1024, leaveOpen: true)
		{
			NewLine = NewlineCrlf,
		};

		WriteToTextWriter(document, writer, options);

		await writer.FlushAsync(cancellationToken);
	}

	public string WriteToString(SidecarDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);

		using StringWriter writer = new()
		{
			NewLine = NewlineCrlf,
		};

		WriteToTextWriter(document, writer, options);
		return writer.ToString();
	}

	private static void WriteToTextWriter(
		SidecarDocument document,
		TextWriter writer,
		IniSidecarWriterOptions options)
	{
		ArgumentNullException.ThrowIfNull(document);
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(options);

		if (options.WriteComments && document.HeaderComment is { Count: > 0 })
		{
			foreach (string headerLine in document.HeaderComment)
			{
				WriteCommentBlock(writer, headerLine, options.PreserveEmptyCommentLines);
			}

			writer.WriteLine();
		}

		foreach (SidecarSection section in document.GetSortedSections())
		{
			if (options.WriteComments && section.Comment is not null)
			{
				WriteCommentBlock(writer, section.Comment, options.PreserveEmptyCommentLines);
			}

			writer.WriteLine($"[{section.Name}]");

			foreach (SidecarProperty property in section.GetSortedProperties())
			{
				if (options.WriteComments && property.Comment is not null)
				{
					WriteCommentBlock(writer, property.Comment, options.PreserveEmptyCommentLines);
				}

				if (property.Value is null && !options.WriteKeysWithNullValues)
				{
					continue;
				}

				writer.WriteLine($"{property.Key}={property.Value ?? string.Empty}");
			}

			// Intentionally keep a blank line after every section, including the last one.
			writer.WriteLine();
		}
	}

	private static void WriteCommentBlock(TextWriter writer, string comment, bool preserveEmptyCommentLines)
	{
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(comment);

		using StringReader reader = new(comment);

		string? line;
		while ((line = reader.ReadLine()) is not null)
		{
			if (line.Length == 0)
			{
				if (preserveEmptyCommentLines)
				{
					writer.WriteLine("#");
				}

				continue;
			}

			writer.WriteLine($"# {line}");
		}
	}
}
