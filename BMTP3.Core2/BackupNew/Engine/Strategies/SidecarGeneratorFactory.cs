using BMTP3.Core2.BackupNew.Api.Request.Enums;

namespace BMTP3.Core2.BackupNew.Engine.Strategies;

public class SidecarGeneratorFactory : ISidecarGeneratorFactory
{
    private readonly IReadOnlyDictionary<SidecarFormat, ISidecarGenerator> _generators;

    public SidecarGeneratorFactory(IReadOnlyDictionary<SidecarFormat, ISidecarGenerator> generators)
    {
        _generators = generators ?? throw new ArgumentNullException(nameof(generators));
    }

    public ISidecarGenerator? Create(SidecarFormat format)
    {
        _generators.TryGetValue(format, out ISidecarGenerator? generator);
        return generator;
    }
}
