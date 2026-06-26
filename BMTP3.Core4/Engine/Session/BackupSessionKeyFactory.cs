using BMTP3.Core4.Api.Models.Enums;
using System.Security.Cryptography;
using System.Text;

namespace BMTP3.Core4.Engine.Session;

/// <summary>
/// Creates backup session state keys from backup plan parameters.
/// </summary>
internal static class BackupSessionKeyFactory
{
	public static BackupSessionKey Create(string sourcePath, string destination, BackupSourceType sourceType)
	{
		ArgumentNullException.ThrowIfNull(sourcePath);
		ArgumentNullException.ThrowIfNull(destination);

		string sourceIdentity = CreateSourceIdentity(sourcePath, destination, sourceType);

		// SessionId is a stable, deterministic hash of the plan identity
		// (source type + source path + destination) so the same plan always
		// finds the same summary file — enabling resume across runs.
		// Different plans (different source or destination) get different IDs,
		// preventing cross-plan summary corruption.
		string sessionId = CreateStableSessionId(sourceIdentity);

		return new BackupSessionKey(sessionId, sourceIdentity);
	}

	private static string CreateSourceIdentity(string sourcePath, string destination, BackupSourceType sourceType)
	{
		return $"{sourcePath.Trim()}:{destination.Trim()}:{sourceType}";
	}

	private static string CreateStableSessionId(string sourceIdentity)
	{
		byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sourceIdentity));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}
}