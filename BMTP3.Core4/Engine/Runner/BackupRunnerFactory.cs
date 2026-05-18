using System;

namespace BMTP3.Core4.Engine.Runner;

internal sealed class BackupRunnerFactory : IBackupRunnerFactory
{
	public IBackupRunner Create(BackupRunnerFactoryCreateRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		int maxDegreeOfParallelism = request.MaxDegreeOfParallelism ?? 1;

		ArgumentOutOfRangeException.ThrowIfNegative(maxDegreeOfParallelism, nameof(request.MaxDegreeOfParallelism));

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
