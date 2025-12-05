using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Job.StateMachine;
/// <summary>
/// Defines a contract for validating and applying state transitions.
/// </summary>
public interface IStateMachine<TState>
{
	/// <summary>
	/// Returns true if the transition from 'from' to 'to' is allowed.
	/// </summary>
	bool CanTransition(TState from, TState to);
}
