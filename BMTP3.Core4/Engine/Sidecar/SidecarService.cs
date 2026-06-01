using BMTP3.Core4.Api.Models.Enums;
using BMTP3.Core4.Hashing;
using System.Text;

namespace BMTP3.Core4.Engine.Sidecar;

internal sealed class SidecarService : ISidecarService
{
	public async Task WriteAsync(string targetFilePath, SidecarRequest request, CancellationToken cancellationToken)
	{
		string content = request.Format switch
		{
			SidecarFormat.Ini => BuildIniContent(request),
			_ => throw new NotImplementedException($"Sidecar format '{request.Format}' is not yet implemented."),
		};

		string sidecarPath = targetFilePath + ".ini";
		await File.WriteAllTextAsync(sidecarPath, content, cancellationToken);
	}

	private static string BuildIniContent(SidecarRequest request)
	{
		StringBuilder sb = new();

		sb.AppendLine("[Settings]");
		sb.AppendLine($"OriginalFileName={request.OriginalFileName}");
		sb.AppendLine($"MediaTakenDateTime={request.AuthoredDateTime?.ToString("O")}");
		sb.AppendLine($"CreateDateTime={request.CreateDateTime?.ToString("O")}");
		sb.AppendLine($"LastAccessDateTime={request.AccessDateTime?.ToString("O")}");
		sb.AppendLine($"LastWriteDateTime={request.ModifyDateTime?.ToString("O")}");
		sb.AppendLine($"RelativePath={request.RelativePath}");
		sb.AppendLine();

		sb.AppendLine("[BackupInfo]");
		sb.AppendLine($"BackupDateTime={request.BackupStartTime:O}");
		sb.AppendLine($"ResolvedDateTime={request.ResolvedDateTime?.ToString("O")}");
		sb.AppendLine();

		if(request.Hashes is { Count: > 0 })
		{
			sb.AppendLine("[Hashes]");
			foreach(KeyValuePair<HashType, string> hash in request.Hashes)
			{
				sb.AppendLine($"{hash.Key}={hash.Value}");
			}
		}

		return sb.ToString();
	}
}
