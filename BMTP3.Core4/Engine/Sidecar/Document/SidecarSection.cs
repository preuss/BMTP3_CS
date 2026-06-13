namespace BMTP3.Core4.Engine.Sidecar.Document;

public sealed class SidecarSection
{
	public string Name { get; }
	public int Weight { get; }
	public string? Comment { get; set; }
	public IEnumerable<SidecarProperty> Properties => _properties;

	private readonly List<SidecarProperty> _properties = [];

	public SidecarSection(string name, int weight = -1, string? comment = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		Name = name;
		Weight = weight;
		Comment = comment;
	}

	public void AddProperty(SidecarProperty property)
	{
		ArgumentNullException.ThrowIfNull(property);
		int existingIndex = _properties.FindIndex(p => string.Equals(p.Key, property.Key, StringComparison.Ordinal));
		if(existingIndex >= 0)
		{
			_properties[existingIndex] = property;
		} else
		{
			_properties.Add(property);
		}
	}

	public SidecarSection WithProperty(string name, string? value, int weight = -1, string? comment = null)
	{
		AddProperty(SidecarProperty.From(name, value, weight, comment));
		return this;
	}

	public SidecarSection WithProperty(string name, DateTime? value, int weight = -1, string? comment = null)
	{
		AddProperty(SidecarProperty.From(name, value, weight, comment));
		return this;
	}

	public SidecarSection WithProperty(string name, DateTimeOffset? value, int weight = -1, string? comment = null)
	{
		AddProperty(SidecarProperty.From(name, value, weight, comment));
		return this;
	}

	public SidecarSection WithProperty(string name, long? value, int weight = -1, string? comment = null)
	{
		AddProperty(SidecarProperty.From(name, value, weight, comment));
		return this;
	}

	public SidecarSection WithProperty(string name, int? value, int weight = -1, string? comment = null)
	{
		AddProperty(SidecarProperty.From(name, value, weight, comment));
		return this;
	}

	public SidecarSection WithProperty(string name, bool? value, int weight = -1, string? comment = null)
	{
		AddProperty(SidecarProperty.From(name, value, weight, comment));
		return this;
	}

	/// Sorts properties so that entries with an explicit weight (>= 0) appear first
	/// in weight order, followed by default-weighted entries sorted alphabetically.
	public IEnumerable<SidecarProperty> GetSortedProperties()
	{
		// If there are any explicitly weighted properties, sort all properties
		// to maintain a predictable output order:
		//   1) weight >= 0 first (false < true), then by weight, then by key
		//   2) weight < 0 last, then by key (weight falls back to 0 within this group)
		if(_properties.Any(p => p.Weight >= 0))
		{
			return _properties
				.OrderBy(p => p.Weight < 0)
				.ThenBy(p => p.Weight < 0 ? 0 : p.Weight)
				.ThenBy(p => p.Key, StringComparer.Ordinal);
		}

		return _properties;
	}
}
