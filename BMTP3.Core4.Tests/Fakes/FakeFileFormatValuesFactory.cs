using BMTP3.Core4.Engine.Strategies;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeFileFormatValuesFactory : IFileFormatValuesFactory
{
	public Dictionary<string, object> Create(FileFormatValuesRequest request)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(request.FileName);
		string extension = Path.GetExtension(request.FileName).TrimStart('.');

		string hashShort = string.Empty;
		string hashMedium = string.Empty;
		string hashLong = string.Empty;

		if(!string.IsNullOrEmpty(request.StrongHash))
		{
			hashLong = request.StrongHash;
			hashMedium = request.StrongHash.Length >= 12 ? request.StrongHash[..12] : request.StrongHash;
			hashShort = request.StrongHash.Length >= 6 ? request.StrongHash[..6] : request.StrongHash;
		}

		return new Dictionary<string, object>(StringComparer.Ordinal)
		{
			["fileName"] = fileNameWithoutExtension,
			["ext"] = extension,
			["extension"] = extension,
			["hashShort"] = hashShort,
			["hashMedium"] = hashMedium,
			["hashLong"] = hashLong,
			["count"] = 1,
		};
	}
}
