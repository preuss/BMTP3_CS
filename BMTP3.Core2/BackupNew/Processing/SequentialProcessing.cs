using BMTP3.Core2.BackupNew.Processing.Steps;

namespace BMTP3.Core2.BackupNew.Processing;

public class SequentialProcessing // : IProcessing
{
	private readonly IBackupStep _preparationStep;
	private readonly IBackupStep _analysisStep;
	private readonly IBackupStep _decisionStep;
	private readonly IBackupStep _storageStep;
	private readonly IBackupStep _finalizationStep;

	public SequentialProcessing(
		IBackupStep preparationStep,
		IBackupStep analysisStep,
		IBackupStep decisionStep,
		IBackupStep storageStep,
		IBackupStep finalizationStep)
	{
		_preparationStep = preparationStep;
		_analysisStep = analysisStep;
		_decisionStep = decisionStep;
		_storageStep = storageStep;
		_finalizationStep = finalizationStep;
	}
}
