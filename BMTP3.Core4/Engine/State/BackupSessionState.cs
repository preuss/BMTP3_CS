using BMTP3.Core4.Models;
using BMTP3.Core4.Models.Enums;

namespace BMTP3.Core4.Engine.State;

/// <summary>
/// Represents the internal in-memory state of a single backup session.
/// The state owns the known backup _items and is the source of truth
/// for what the engine knows about the session.
/// </summary>
internal sealed class BackupSessionState
{
	private readonly List<BackupItem> _items = new();

	public BackupSessionState(string sessionId, string sourceIdentity)
	{
		if(string.IsNullOrWhiteSpace(sessionId))
		{
			throw new ArgumentException("Session id is required.", nameof(sessionId));
		}

		if(string.IsNullOrWhiteSpace(sourceIdentity))
		{
			throw new ArgumentException("Source identity is required.", nameof(sourceIdentity));
		}

		SessionId = sessionId;
		SourceIdentity = sourceIdentity;
		Phase = BackupPhase.Starting;
	}

	/// <summary>
	/// Unique identifier for this backup session.
	/// </summary>
	public string SessionId { get; }

	/// <summary>
	/// Identity of the source this session state belongs to.
	/// </summary>
	public string SourceIdentity { get; }

	/// <summary>
	/// The current high-level phase of the backup job.
	/// </summary>
	public BackupPhase Phase { get; private set; }

	/// <summary>
	/// All _items known to the backup session.
	/// </summary>
	public IReadOnlyList<BackupItem> Items => _items;

	/// <summary>
	/// Optional terminal failure reason if the backup ends in Failed state.
	/// </summary>
	public BackupErrorCode? FailureReason { get; private set; }

	public void SetPhase(BackupPhase phase)
	{
		Phase = phase;
	}

	public void Fail(BackupErrorCode failureReason)
	{
		Phase = BackupPhase.Failed;
		FailureReason = failureReason;
	}

	public void Cancel()
	{
		Phase = BackupPhase.Cancelled;
		FailureReason = null;
	}

	public void Complete()
	{
		Phase = BackupPhase.Completed;
		FailureReason = null;
	}

	public void AddItem(BackupItem item)
	{
		ArgumentNullException.ThrowIfNull(item);

		if(_items.Any(existing => existing.Id == item.Id))
		{
			throw new InvalidOperationException($"A backup item with id '{item.Id}' already exists.");
		}

		_items.Add(item);
	}

	public IEnumerable<BackupItem> GetPendingItems()
	{
		return _items.Where(item => item.Status == BackupItemStatus.Pending);
	}
}