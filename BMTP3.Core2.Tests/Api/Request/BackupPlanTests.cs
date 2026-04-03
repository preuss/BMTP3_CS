using BMTP3.Core2.BackupNew.Api.Request;

namespace BMTP3.Core2.Tests.Api.Request;

public class BackupPlanTests
{
    [Fact]
    public void Normalize_ClampsVerificationValues()
    {
        var plan = new BackupPlan { VerificationRetryCount = 0, VerificationRetryDelayMs = -10, VerificationTimeoutMs = -1 };
        plan.Normalize();
        Assert.Equal(1, plan.VerificationRetryCount);
        Assert.Equal(0, plan.VerificationRetryDelayMs);
        Assert.Equal(0, plan.VerificationTimeoutMs);
    }
}
