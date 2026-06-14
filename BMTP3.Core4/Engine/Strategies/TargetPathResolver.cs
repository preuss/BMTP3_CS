using BMTP3.Common.MessageFormatterParser;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Helpers;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed class TargetPathResolver : ITargetPathResolver
{
	private readonly IMessageFormatter _formatter;
	private readonly IFileFormatValuesFactory _formatValuesFactory;

	public TargetPathResolver(IMessageFormatter formatter, IFileFormatValuesFactory formatValuesFactory)
	{
		_formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		_formatValuesFactory = formatValuesFactory ?? throw new ArgumentNullException(nameof(formatValuesFactory));
	}

	public string Resolve(TargetPathResolveRequest request)
	{
		return request.OutputStructureStrategy switch
		{
			OutputStructureStrategy.PreserveHierarchy => ResolvePreserveHierarchy(request.DestinationRoot, request.RelativeDirectoryPath, request.FileName),
			OutputStructureStrategy.Flat => ResolveFlat(request.DestinationRoot, request.FileName),
			OutputStructureStrategy.CustomPathPattern => ResolveCustomPathPattern(request),
			_ => throw new ArgumentOutOfRangeException(nameof(request.OutputStructureStrategy), request.OutputStructureStrategy, "Unknown output structure strategy."),
		};
	}

	private static string ResolvePreserveHierarchy(string destinationRoot, string? relativeDirectoryPath, string fileName)
	{
		string normalizedRelativeDirectoryPath = NormalizeRelativeDirectoryPath(relativeDirectoryPath);

		if(normalizedRelativeDirectoryPath.Length == 0)
		{
			return Path.Combine(destinationRoot, fileName);
		}

		return Path.Combine(destinationRoot, normalizedRelativeDirectoryPath, fileName);
	}

	private static string ResolveFlat(string destinationRoot, string fileName)
	{
		return Path.Combine(destinationRoot, fileName);
	}

	private string ResolveCustomPathPattern(TargetPathResolveRequest request)
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
			DeviceName: null,
			DeviceModel: null);

		Dictionary<string, object> values = _formatValuesFactory.Create(valuesRequest);

		PreparedMessageFormat prepared = new(_formatter, request.CustomPattern, values);

		string relativePath = prepared.Format();
		relativePath = PathHelper.NormalizeCustomRelativePath(relativePath);

		return Path.Combine(request.DestinationRoot, relativePath);
	}

	private static string NormalizeRelativeDirectoryPath(string? relativeDirectoryPath)
	{
		return PathHelper.NormalizeCustomRelativePath(relativeDirectoryPath, normalizeNull: true);
	}
}