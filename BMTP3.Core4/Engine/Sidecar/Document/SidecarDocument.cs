namespace BMTP3.Core4.Engine.Sidecar.Document;

public sealed class SidecarDocument
{
	public List<string>? HeaderComment { get; set; }
	public IEnumerable<SidecarSection> Sections => _sections.Values;

	private readonly Dictionary<string, SidecarSection> _sections = new();

	public SidecarSection? GetSection(string name)
	{
		_sections.TryGetValue(name, out SidecarSection? section);
		return section;
	}

	public void AddSection(SidecarSection section)
	{
		ArgumentNullException.ThrowIfNull(section);
		_sections[section.Name] = section;
	}

	public SidecarSection WithSection(string name, int weight = 100, string? comment = null)
	{
		return GetOrCreateSection(name, weight, comment);
	}

	private SidecarSection GetOrCreateSection(string name, int weight = 100, string? comment = null)
	{
		if (_sections.TryGetValue(name, out SidecarSection? existing))
			return existing;

		SidecarSection section = new(name, weight, comment);
		_sections[name] = section;
		return section;
	}

	public IEnumerable<SidecarSection> GetSortedSections() =>
		_sections.Values
			.OrderBy(s => s.Weight < 0)
			.ThenBy(s => s.Weight < 0 ? 0 : s.Weight)
			.ThenBy(s => s.Name, StringComparer.Ordinal);
}
