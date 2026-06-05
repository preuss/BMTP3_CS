using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.SignalInterrupts;

[Flags]
public enum SignalInterruptKind
{
	None = 0,

	Interrupt = 1 << 0,      // Ctrl+C / SIGINT
	Break = 1 << 1,          // Ctrl+Break / SIGQUIT-ish
	ConsoleClose = 1 << 2,   // Console window close / CTRL_CLOSE_EVENT
	Logoff = 1 << 3,         // User logoff / CTRL_LOGOFF_EVENT
	Shutdown = 1 << 4,       // System shutdown / CTRL_SHUTDOWN_EVENT

	All = Interrupt | Break | ConsoleClose | Logoff | Shutdown
}
