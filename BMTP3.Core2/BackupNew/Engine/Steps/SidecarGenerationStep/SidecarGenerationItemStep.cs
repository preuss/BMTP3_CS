using BMTP3.Core2.BackupNew.Api.Enums;
using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;

namespace BMTP3.Core2.BackupNew.Engine.Steps.SidecarGenerationStep;

/// <summary>
/// Generates sidecar files for items using the configured <see cref="ISidecarGenerator"/>.
/// </summary>
public class SidecarGenerationItemStep : IBackupItemStep<BackupPlan, bool>
{
	public string Name => "Sidecar Generation";
	public FilePhase Phase => FilePhase.Metadata;

	private readonly BackupPlan _context;
	public BackupPlan Context => _context;

    private readonly ISidecarGeneratorFactory _factory;

    /// <summary>
    /// Initializes a new instance of <see cref="SidecarGenerationItemStep"/>.
    /// The step will resolve the appropriate ISidecarGenerator implementation from the provided factory based on the BackupPlan.SidecarFormat.
    /// </summary>
    public SidecarGenerationItemStep(BackupPlan context, ISidecarGeneratorFactory factory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

	/// <summary>
	/// Executes sidecar generation for a single item.
	/// </summary>
	/// <param name="item">The backup item.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>True if a sidecar was generated; otherwise false.</returns>
	public async Task<bool> ExecuteAsync(IBackupItem item, IProgress<ulong> progress, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);

		try
		{
			// DryRun: skip sidecar generation completely
			if(_context.DryRun)
			{
				item.AddLog("Sidecar skipped (DryRun).", Name);
				return true;
			}

            // Resolve generator based on configured sidecar format in the plan
            ISidecarGenerator? generator = _factory.Create(_context.SidecarFormat);
            if (generator == null)
            {
                item.AddLog("No sidecar generator available.", Name);
                return true; // Not an error - sidecar is optional
            }

            bool generated = await generator.GenerateAsync(item, ct);

			if(generated)
			{
				item.AddLog("Sidecar generated.", Name);
			} else
			{
				// Sidecar is critical - fail the item when generation fails
				item.Fail("Sidecar generation failed: generator returned false", Name);
			}

			return generated;
		} catch(OperationCanceledException)
		{
			throw;
		} catch(Exception ex)
		{
			// Sidecar is critical - fail the item when generation throws
			item.Fail($"Sidecar generation failed: {ex.Message}", Name);
			return false;
		}
	}
}
