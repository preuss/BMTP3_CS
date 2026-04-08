namespace BMTP3.Core2.BackupNew.Engine.StateMachine;

/// <summary>
///     Defines a contract for validating and applying state transitions.
/// </summary>
public interface IStateMachine<TState>
{
	/// <summary>
	///     Returns true if the transition from 'from' to 'to' is allowed.
	/// </summary>
	bool CanTransition(TState from, TState to);
}