using BMTP3.Common.MessageFormatterParser;
using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Engine.Compare;
using BMTP3.Core4.Engine.Hashing;
using BMTP3.Core4.Hashing;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.Strategies;

internal sealed class RenameCollisionResolver : IRenameCollisionResolver
{
	private const int MaxAttempts = 1000;

	private readonly IFileCompareService _fileCompareService;
	private readonly IHashService _hashService;
	private readonly IMessageFormatter _formatter;
	private readonly IFileFormatValuesFactory _formatValuesFactory;

	public RenameCollisionResolver(
		IFileCompareService fileCompareService,
		IHashService hashService,
		IMessageFormatter formatter,
		IFileFormatValuesFactory formatValuesFactory)
	{
		_fileCompareService = fileCompareService ?? throw new ArgumentNullException(nameof(fileCompareService));
		_hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
		_formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
		_formatValuesFactory = formatValuesFactory ?? throw new ArgumentNullException(nameof(formatValuesFactory));
	}

	public async Task<RenameCollisionResult> ResolveAsync(RenameCollisionRequest request, CancellationToken ct)
	{
		string dir = Path.GetDirectoryName(request.IntendedTargetPath)
			?? throw new InvalidOperationException("IntendedTargetPath has no directory component.");

		string nameWithoutExt = Path.GetFileNameWithoutExtension(request.IntendedTargetPath);
		string extension = Path.GetExtension(request.IntendedTargetPath);

		// First check the existing intended target.
		if(File.Exists(request.IntendedTargetPath))
		{
			bool intendedIsIdentical = await ContentCompareAsync(
				request.SourcePath,
				request.IntendedTargetPath,
				request,
				ct);

			if(intendedIsIdentical)
			{
				return new RenameCollisionResult(CollisionResolutionAction.Skip, request.IntendedTargetPath);
			}
		}

		for(int count = 1; count <= MaxAttempts; count++)
		{
			string candidateTargetPath = GenerateCandidateTargetPath(
				dir,
				nameWithoutExt,
				extension,
				count,
				request
			);

			if(!File.Exists(candidateTargetPath))
			{
				return new RenameCollisionResult(CollisionResolutionAction.Move, candidateTargetPath);
			}

			bool candidateIsIdentical = await ContentCompareAsync(
				request.SourcePath,
				candidateTargetPath,
				request,
				ct
			);

			if(candidateIsIdentical)
			{
				return new RenameCollisionResult(CollisionResolutionAction.Skip, candidateTargetPath);
			}
		}

		throw new IOException($"Could not resolve rename collision after {MaxAttempts} attempts for: {request.IntendedTargetPath}");
	}

	private string GenerateCandidateTargetPath(
		string dir,
		string nameWithoutExt,
		string extension,
		int count,
		RenameCollisionRequest request
	)
	{
		return request.RenameStrategy switch
		{
			RenameStrategy.Increment => Path.Combine(dir, $"{nameWithoutExt}_{count}{extension}"),
			RenameStrategy.Timestamp => Path.Combine(dir, $"{nameWithoutExt}_{request.CreateFileDate:yyyyMMdd_HHmmss}_{count}{extension}"),
			RenameStrategy.Hash => Path.Combine(dir, $"{nameWithoutExt}_{GetHashShort(request)}_{count}{extension}"),
			RenameStrategy.Custom => GenerateCustomCandidate(dir, count, request),
			_ => throw new ArgumentOutOfRangeException(nameof(request.RenameStrategy), request.RenameStrategy, "Unknown rename strategy."),
		};
	}

	private string GenerateCustomCandidate(string dir, int count, RenameCollisionRequest request)
	{
		if(string.IsNullOrWhiteSpace(request.CustomRenamePattern))
		{
			throw new InvalidOperationException("Custom rename pattern is required when RenameStrategy is Custom.");
		}

		FileFormatValuesRequest valuesRequest = new(
			FileName: request.FileName,
			RelativePath: request.RelativePath,
			CreateFileDate: request.CreateFileDate,
			ItemId: request.ItemId,
			StrongHash: request.StrongHash,
			DeviceName: request.DeviceName,
			DeviceModel: request.DeviceModel);

		Dictionary<string, object> baseValues = _formatValuesFactory.Create(valuesRequest);

		PreparedMessageFormat prepared = new(
			_formatter,
			request.CustomRenamePattern,
			baseValues);

		string relative = prepared.Format(new Dictionary<string, object>(StringComparer.Ordinal)
		{
			["count"] = count.ToString(),
		});

		relative = NormalizeCustomRelativePath(relative);

		return Path.Combine(dir, relative);
	}

	private static string NormalizeCustomRelativePath(string relativePath)
	{
		if(string.IsNullOrWhiteSpace(relativePath))
		{
			throw new InvalidOperationException("Custom rename pattern produced an empty path.");
		}

		string normalized = relativePath
			.Trim()
			.Replace('\\', Path.DirectorySeparatorChar)
			.Replace('/', Path.DirectorySeparatorChar);

		if(normalized.Length >= 2 && normalized[1] == ':')
		{
			throw new InvalidOperationException("Custom rename pattern produced an absolute path. Remove drive letter.");
		}

		normalized = normalized.Trim(Path.DirectorySeparatorChar);

		if(normalized.Length == 0)
		{
			throw new InvalidOperationException("Custom rename pattern produced an empty path.");
		}

		string[] parts = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

		if(parts.Any(part => part == ".."))
		{
			throw new InvalidOperationException("Custom rename pattern must not contain parent directory traversal.");
		}

		return string.Join(Path.DirectorySeparatorChar, parts);
	}

	private static string GetHashShort(RenameCollisionRequest request)
	{
		if(string.IsNullOrWhiteSpace(request.StrongHash))
		{
			throw new InvalidOperationException("RenameStrategy.Hash requires StrongHash.");
		}

		return request.StrongHash.Length >= 6
			? request.StrongHash[..6]
			: request.StrongHash;
	}

	private async Task<bool> ContentCompareAsync(
		string sourcePath,
		string candidatePath,
		RenameCollisionRequest request,
		CancellationToken ct
	)
	{
		return request.ComparisonType switch
		{
			CollisionComparisonType.Binary => await _fileCompareService.CompareAsync(sourcePath, candidatePath, ct),
			CollisionComparisonType.Hash => await HashCompareAsync(candidatePath, request, ct),
			CollisionComparisonType.None => false,
			_ => throw new ArgumentOutOfRangeException(nameof(request.ComparisonType), request.ComparisonType, "Unknown collision comparison type."),
		};
	}

	private async Task<bool> HashCompareAsync(
		string candidatePath,
		RenameCollisionRequest request,
		CancellationToken ct
	)
	{
		IReadOnlyList<HashAlgorithmType> hashTypes = request.ComparisonHashAlgorithmTypes;

		if(hashTypes.Count == 0)
		{
			return false;
		}

		// TODO N1: temp hash already in request.StrongHash, but target hash is re-computed from disk below.
		// If target has a sidecar from a previous run, read that hash instead.
		try
		{
			FileContent candidateContent = new(candidatePath);

			Dictionary<HashType, string> candidateHashes = await _hashService.ComputeHashesAsync(
				candidateContent,
				relativePath: "",
				hashTypes,
				progress: null,
				cancellationToken: ct
			);

			foreach(HashAlgorithmType algorithmType in hashTypes)
			{
				HashType hashType = ToHashType(algorithmType);

				if(!candidateHashes.TryGetValue(hashType, out string? candidateHash))
				{
					return false;
				}

				if(!string.Equals(request.StrongHash, candidateHash, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			return true;
		} catch
		{
			return false;
		}
	}

	private static HashType ToHashType(HashAlgorithmType algorithmType) => algorithmType switch
	{
		HashAlgorithmType.SHA2_256 => HashType.SHA2_256,
		HashAlgorithmType.SHA2_512 => HashType.SHA2_512,
		HashAlgorithmType.SHA3_256_FIPS202 => HashType.SHA3_256_FIPS202,
		HashAlgorithmType.SHA3_512_FIPS202 => HashType.SHA3_512_FIPS202,
		HashAlgorithmType.SHA3_256_KECCAK => HashType.SHA3_256_KECCAK,
		HashAlgorithmType.SHA3_512_KECCAK => HashType.SHA3_512_KECCAK,
		HashAlgorithmType.MD5_128 => HashType.MD5_128,
		HashAlgorithmType.BLAKE3_256 => HashType.BLAKE3_256,
		HashAlgorithmType.BLAKE3_512 => HashType.BLAKE3_512,
		_ => throw new ArgumentOutOfRangeException(nameof(algorithmType), algorithmType, null),
	};
}