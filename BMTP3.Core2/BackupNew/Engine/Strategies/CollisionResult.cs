using BMTP3.Core2.BackupNew.Api.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;
/// <summary>
/// Result of a collision resolution attempt.
/// </summary>
public record CollisionResult(
	BackupActionType Action,
	string TargetPath,
	string Reason
);