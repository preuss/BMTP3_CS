using BMTP3.Core2.BackupNew.Api.UI;
using Spectre.Console;
using System.Threading;
using System.Threading.Tasks;

namespace BMTP3.Consoles.UI;

public class SpectreConsolePrompter : IUserPrompter
{
    private readonly IAnsiConsole _console;
    public SpectreConsolePrompter(IAnsiConsole console) => _console = console;

    public Task<bool> ConfirmAsync(string message, CancellationToken cancellationToken = default)
    {
        // Spectre's Confirm is synchronous; wrap in Task to satisfy interface.
        bool result = _console.Confirm(message);
        return Task.FromResult(result);
    }
}
