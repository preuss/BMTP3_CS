using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core4.Engine.Runner;
internal interface IBackupRunnerFactory
{
	IBackupRunner Create(BackupRunnerFactoryCreateRequest request);
}
