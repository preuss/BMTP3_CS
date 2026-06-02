using BMTP3.Core4.Engine.Strategies;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeFileFormatValuesFactory : IFileFormatValuesFactory
{
	public Dictionary<string, object> Create(FileFormatValuesRequest request)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(request.FileName);
		string extension = Path.GetExtension(request.FileName).TrimStart('.');

		return new Dictionary<string, object>(StringComparer.Ordinal)
		{
			["fileName"] = fileNameWithoutExtension,
			["ext"] = extension,
			["extension"] = extension,
			["count"] = 1,
		};
	}
}
