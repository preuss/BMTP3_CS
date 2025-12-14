using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Domain.Item;
using BMTP3.Core2.BackupNew.Engine.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Steps.HashStep;

public class HashItemStep : IBackupItemStep<HashStepContext, HashStepResult>
{
	public string Name => "Hashing";

	private readonly HashStepContext _context;
	public HashStepContext Context => _context;

	public HashItemStep(HashStepContext context)
	{
		_context = context;
	}

	public Task<HashStepResult> ExecuteAsync(IBackupItem item, CancellationToken ct)
	{
		throw new NotImplementedException();
	}
}
