using BMTP3.Core4.Api.Models.Enums;

namespace BMTP3.Core4.Engine;

internal enum CollisionResolution
{
	Overwrite,
	Rename,
	Skip,
	Error
}

internal static class CollisionHelpers
{
	internal static string ResolveTargetPath(
		string destinationDir,
		string fileName,
		CollisionStrategy strategy,
		out CollisionResolution resolution)
	{
		string targetPath = Path.Combine(destinationDir, fileName);

		if(!File.Exists(targetPath))
		{
			resolution = CollisionResolution.Overwrite;
			return targetPath;
		}

		return strategy switch
		{
			CollisionStrategy.Overwrite => Overwrite(targetPath, out resolution),
			CollisionStrategy.Rename => Rename(targetPath, out resolution),
			CollisionStrategy.Skip => Skip(targetPath, out resolution),
			CollisionStrategy.Error => Error(targetPath, out resolution),
			_ => Error(targetPath, out resolution),
		};
	}

	internal static string GenerateUniquePath(string targetPath)
	{
		string? directory = Path.GetDirectoryName(targetPath);
		string nameWithoutExt = Path.GetFileNameWithoutExtension(targetPath);
		string extension = Path.GetExtension(targetPath);

		for(int i = 1; i < 1000; i++)
		{
			string candidate = Path.Combine(directory!, $"{nameWithoutExt}_{i}{extension}");
			if(!File.Exists(candidate))
			{
				return candidate;
			}
		}

		return targetPath;
	}

	private static string Overwrite(string targetPath, out CollisionResolution r)
	{
		r = CollisionResolution.Overwrite;
		return targetPath;
	}

	private static string Rename(string targetPath, out CollisionResolution r)
	{
		r = CollisionResolution.Rename;
		return GenerateUniquePath(targetPath);
	}

	private static string Skip(string targetPath, out CollisionResolution r)
	{
		r = CollisionResolution.Skip;
		return targetPath;
	}

	private static string Error(string targetPath, out CollisionResolution r)
	{
		r = CollisionResolution.Error;
		return targetPath;
	}
}
