using BMTP3.Core2.BackupNew.Api.Request;
using BMTP3.Core2.BackupNew.Api.Request.Enums;

namespace BMTP3.Core2.Tests.Api.Request;

/// <summary>
/// Additional Normalize() tests that are NOT covered by BackupPlanTests.cs.
/// BackupPlanTests already covers clamping of verification numeric fields.
/// These tests cover HashTypes defaults, collision setting defaults, and
/// protection of already-valid values.
/// </summary>
public class BackupPlanNormalizeTests
{
    // ── HashTypes ────────────────────────────────────────────────────────────

    [Fact]
    public void Normalize_HashTypes_DefaultsAreNonEmpty_WhenCreatedWithDefaultConstructor()
    {
        // BackupPlan sets up a non-empty default hash-type set at construction time.
        // After Normalize(), the set must remain non-empty.
        var plan = new BackupPlan();
        plan.Normalize();

        Assert.NotNull(plan.HashTypes);
        Assert.NotEmpty(plan.HashTypes);
    }

    [Fact]
    public void Normalize_DoesNotOverwrite_ExistingHashTypes()
    {
        // If a consumer explicitly sets exactly one hash type, Normalize() must
        // leave that choice intact and not reset it to a wider default set.
        var plan = new BackupPlan();
        plan.HashTypes = new HashSet<HashType> { HashType.SHA2_256 };

        plan.Normalize();

        Assert.Single(plan.HashTypes);
        Assert.Contains(HashType.SHA2_256, plan.HashTypes);
    }

    [Fact]
    public void Normalize_MultipleHashTypes_AllPreserved()
    {
        // Multiple explicitly configured hash types must all survive a Normalize() call.
        var chosen = new HashSet<HashType> { HashType.MD5_128, HashType.SHA2_512, HashType.BLAKE3_256 };
        var plan = new BackupPlan { HashTypes = chosen };

        plan.Normalize();

        Assert.Equal(3, plan.HashTypes.Count);
        Assert.Contains(HashType.MD5_128, plan.HashTypes);
        Assert.Contains(HashType.SHA2_512, plan.HashTypes);
        Assert.Contains(HashType.BLAKE3_256, plan.HashTypes);
    }

    // ── Collision / Resolution defaults ─────────────────────────────────────

    [Fact]
    public void Normalize_CollisionResolution_DefaultIsRename()
    {
        // The default CollisionResolution set at property-initializer time is Rename.
        // Normalize() must not change it.
        var plan = new BackupPlan();
        plan.Normalize();

        Assert.Equal(CollisionResolutionType.Rename, plan.CollisionResolution);
    }

    [Fact]
    public void Normalize_CollisionResolution_ExplicitValuePreserved()
    {
        // An explicitly configured resolution (e.g. Skip) must survive Normalize().
        var plan = new BackupPlan { CollisionResolution = CollisionResolutionType.Skip };
        plan.Normalize();

        Assert.Equal(CollisionResolutionType.Skip, plan.CollisionResolution);
    }

    [Fact]
    public void Normalize_ComparisonType_DefaultIsBinary()
    {
        var plan = new BackupPlan();
        plan.Normalize();

        Assert.Equal(CollisionComparisonType.Binary, plan.ComparisonType);
    }

    // ── PostWriteVerification default ────────────────────────────────────────

    [Fact]
    public void Normalize_SetsDefaultPostWriteVerification_WhenUndefinedEnumValue()
    {
        // Normalize() must clamp an out-of-range PostWriteVerificationType back to Hash.
        var plan = new BackupPlan
        {
            PostWriteVerification = (PostWriteVerificationType)999
        };
        plan.Normalize();

        Assert.Equal(PostWriteVerificationType.Hash, plan.PostWriteVerification);
    }

    [Fact]
    public void Normalize_DoesNotOverwrite_ValidPostWriteVerification()
    {
        var plan = new BackupPlan { PostWriteVerification = PostWriteVerificationType.Binary };
        plan.Normalize();

        Assert.Equal(PostWriteVerificationType.Binary, plan.PostWriteVerification);
    }
}
