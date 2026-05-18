using System;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class BackupRunnerFactory : IBackupRunnerFactory
{
	public IBackupRunner Create(BackupRunnerFactoryCreateRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(request.MaxDegreeOfParallelism);
		ArgumentOutOfRangeException.ThrowIfNegative(request.MaxDegreeOfParallelism.Value);

		int maxDegreeOfParallelism = request.MaxDegreeOfParallelism.Value;

		if(maxDegreeOfParallelism > 2)
		{
			return new ParallelBackupRunner();
		}

		if(maxDegreeOfParallelism == 2)
		{
			return new LimitedParallelBackupRunner();
		}

		return new SequentialBackupRunner();
	}
}
