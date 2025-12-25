using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using System;
using System.Threading;
using System.Threading.Tasks;

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

	private readonly ISidecarGenerator _generator;

	/// <summary>
	/// Initializes a new instance of <see cref="SidecarGenerationItemStep"/>.
	/// </summary>
	/// <param name="context">Backup plan context.</param>
	/// <param name="generator">Sidecar generator implementation.</param>
	public SidecarGenerationItemStep(BackupPlan context, ISidecarGenerator generator)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_generator = generator ?? throw new ArgumentNullException(nameof(generator));
	}

	/// <summary>
	/// Executes sidecar generation for a single item.
	/// </summary>
	/// <param name="item">The backup item.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>True if a sidecar was generated; otherwise false.</returns>
	public async Task<bool> ExecuteAsync(IBackupItem item, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);

		try
		{
			bool generated = await _generator.GenerateAsync(item, ct);

			if(generated)
			{
				item.AddLog("Sidecar generated.", Name);
			} else
			{
				item.AddLog("No sidecar produced.", Name);
			}

			return generated;
		} catch(OperationCanceledException)
		{
			throw;
		} catch(Exception ex)
		{
			item.Fail($"Sidecar generation failed: {ex.Message}", Name);
			return false;
		}
	}
}
