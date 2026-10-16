using BMTP3.Common.MessageFormatterParser;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Helpers;
using Microsoft.Extensions.Logging;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed class TargetPathResolver : ITargetPathResolver
{
	private readonly IMessageFormatter _formatter;
	private readonly IFileFormatValuesFactory _formatValuesFactory;
	private readonly ILogger<TargetPathResolver> _logger;

	public TargetPathResolver(
		IMessageFormatter formatter,
		IFileFormatValuesFactory formatValuesFactory,
		ILogger<TargetPathResolver> logger
	)
	{
		_formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		_formatValuesFactory = formatValuesFactory ?? throw new ArgumentNullException(nameof(formatValuesFactory));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public string Resolve(TargetPathResolveRequest request)
	{
		return request.OutputStructureStrategy switch
		{
			OutputStructureStrategy.PreserveHierarchy => ResolvePreserveHierarchy(request.DestinationRoot, request.RelativeDirectoryPath, request.FileName, _logger),
			OutputStructureStrategy.Flat => ResolveFlat(request.DestinationRoot, request.FileName, _logger),
			OutputStructureStrategy.CustomPathPattern => ResolveCustomPathPattern(request, _formatter, _formatValuesFactory, _logger),
			_ => throw new ArgumentOutOfRangeException(nameof(request.OutputStructureStrategy), request.OutputStructureStrategy, "Unknown output structure strategy."),
		};
	}

	private static string ResolvePreserveHierarchy(string destinationRoot, string? relativeDirectoryPath, string fileName, ILogger logger)
	{
		// Combine the source-derived parts, then sanitize once: sanitizing the whole relative path
		// covers every segment. The destination root is combined afterwards because its drive colon
		// is legal there and must never be sanitized.
		string relativePath = string.IsNullOrWhiteSpace(relativeDirectoryPath)
			? fileName
			: Path.Combine(relativeDirectoryPath, fileName);
		string sanitizedRelativePath = SanitizeRelativePath(relativePath);
		LogIfSanitizedChange(logger, relativePath, sanitizedRelativePath, "Relative path");

		string finalRelativePath = NormalizeRelativeDirectoryPath(sanitizedRelativePath);

		return Path.Combine(destinationRoot, finalRelativePath);
	}

	private static string ResolveFlat(string destinationRoot, string fileName, ILogger logger)
	{
		string sanitizedFileName = SanitizeRelativePath(fileName);

		LogIfSanitizedChange(logger, fileName, sanitizedFileName, "File name");

		return Path.Combine(destinationRoot, sanitizedFileName);
	}

	private static string ResolveCustomPathPattern(
		TargetPathResolveRequest request,
		IMessageFormatter formatter,
		IFileFormatValuesFactory formatValuesFactory,
		ILogger logger
	)
	{
		if(string.IsNullOrWhiteSpace(request.CustomPattern))
		{
			throw new InvalidOperationException("Custom output pattern is required when OutputStructureStrategy is CustomPathPattern.");
		}

		FileFormatValuesRequest valuesRequest = new(
			FileName: request.FileName,
			RelativeFilePath: request.RelativeDirectoryPath,
			CreateFileDate: request.CreateFileDate,
			ItemId: request.ItemId,
			StrongHash: request.StrongHash,
			SourceDetails: request.SourceDetails
		);

		Dictionary<string, object> values = formatValuesFactory.Create(valuesRequest);

		PreparedMessageFormat prepared = new(formatter, request.CustomPattern, values);

		string relativePath = prepared.Format();
		string sanitizedRelativePath = SanitizeRelativePath(relativePath);
		LogIfSanitizedChange(logger, relativePath, sanitizedRelativePath, "Custom path output");

		string finalRelativePath = NormalizeRelativeDirectoryPath(sanitizedRelativePath);

		return Path.Combine(request.DestinationRoot, finalRelativePath);
	}

	private static string NormalizeRelativeDirectoryPath(string? relativeDirectoryPath)
	{
		return PathHelper.NormalizeCustomRelativePath(relativeDirectoryPath, normalizeNull: true);
	}

	private static string SanitizeRelativePath(string relativePath)
	{
		return PathHelper.SanitizeRelativePath(relativePath);
	}

	private static void LogIfSanitizedChange(ILogger logger, string? original, string sanitized, string label)
	{
		if(original is null || string.Equals(original, sanitized, StringComparison.Ordinal))
		{
			return;
		}

		logger.LogDebug("{Label} sanitized for destination: '{Original}' -> '{Sanitized}'", label, original, sanitized);
	}
}