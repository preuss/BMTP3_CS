using BMTP3.Core4.Helpers;
using BMTP3.Core4.Models;

namespace BMTP3.Core4.Engine.State;

/// <summary>
/// Creates backup session state keys from backup plans.
/// </summary>
internal static class BackupSessionStateKeyFactory
{
	public static BackupSessionStateKey Create(BackupPlan plan)
	{
		plan = Guard.RequireNonNull(plan);

		string sessionId = Guid.NewGuid().ToString("N");
		string sourceIdentity = CreateSourceIdentity(plan);

		return new BackupSessionStateKey(sessionId, sourceIdentity);
	}

	private static string CreateSourceIdentity(BackupPlan plan)
	{
		return $"{plan.SourceType}:{plan.Source.Trim()}";
	}
}