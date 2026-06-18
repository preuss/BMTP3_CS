using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed class FileFormatValuesFactory : IFileFormatValuesFactory
{
	public Dictionary<string, object> Create(FileFormatValuesRequest request)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(request.FileName);
		string fileNameWithExtension = request.FileName;
		string extension = Path.GetExtension(fileNameWithExtension).TrimStart('.');
		string relativeFilePath = (request.RelativeFilePath ?? string.Empty)
			.Trim('/', '\\')
			.Replace('\\', '/');

		long subSecondTicks = request.CreateFileDate.Ticks % 10_000_000L;
		string frac = subSecondTicks.ToString("D7");

		string frac1 = frac[..1];
		string frac2 = frac[..2];
		string frac3 = frac[..3];
		string frac4 = frac[..4];
		string frac5 = frac[..5];
		string frac6 = frac[..6];
		string frac7 = frac;
		string frac8 = frac + "0";
		string frac9 = frac + "00";

		string hashShort = string.Empty;
		string hashMedium = string.Empty;
		string hashLong = string.Empty;

		if(!string.IsNullOrEmpty(request.StrongHash))
		{
			hashLong = request.StrongHash;
			hashMedium = request.StrongHash.Length >= 12 ? request.StrongHash[..12] : request.StrongHash;
			hashShort = request.StrongHash.Length >= 6 ? request.StrongHash[..6] : request.StrongHash;
		}

		DateTimeOffset selectedFileDate = request.CreateFileDate;
		string itemId = request.ItemId;

		Dictionary<string, object> dict = new(StringComparer.Ordinal)
		{
			// --- Date / time ---
			["YYYY"] = selectedFileDate.Year.ToString("D4"),
			["MM"] = selectedFileDate.Month.ToString("D2"),
			["DD"] = selectedFileDate.Day.ToString("D2"),
			["hh"] = selectedFileDate.Hour.ToString("D2"),
			["mm"] = selectedFileDate.Minute.ToString("D2"),
			["ss"] = selectedFileDate.Second.ToString("D2"),

			// --- Fractional seconds (f) ---
			["f"] = frac1,
			["ff"] = frac2,
			["fff"] = frac3,
			["ffff"] = frac4,
			["fffff"] = frac5,
			["ffffff"] = frac6,
			["fffffff"] = frac7,
			["ffffffff"] = frac8,
			["fffffffff"] = frac9,

			// --- Fractional seconds (S, alias for f) ---
			["S"] = frac1,
			["SS"] = frac2,
			["SSS"] = frac3,
			["SSSS"] = frac4,
			["SSSSS"] = frac5,
			["SSSSSS"] = frac6,
			["SSSSSSS"] = frac7,
			["SSSSSSSS"] = frac8,
			["SSSSSSSSS"] = frac9,

			// --- Filename ---
			["fileName"] = fileNameWithoutExtension,
			["fullFileName"] = fileNameWithExtension,
			["ext"] = extension,
			["extension"] = extension, // Alias for ext

			// --- Path ---
			["relativePath"] = relativeFilePath,
			["sourceRelativePath"] = relativeFilePath, // Alias for relativePath
			["sourceStructure"] = relativeFilePath, // Alias for relativePath

			// --- Metadata ---
			["itemId"] = itemId,

			// --- Hash ---
			["hashShort"] = hashShort,
			["hashMedium"] = hashMedium,
			["hashLong"] = hashLong,
		};

		if(request.SourceDetails is MediaDeviceDriveSourceDetails mdd)
		{
			dict["deviceName"] = mdd.FriendlyName;
			dict["deviceModel"] = mdd.Model;
		}

		return dict;
	}

	private static string NotYetImplemented(string token) =>
		throw new NotImplementedException($"${{{token}}} requires plan-level data — not yet available");
}
