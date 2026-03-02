namespace BMTP3.Core.Metadata.SideCar {
	/// <summary>
	/// Represents a section in a SideCar document
	/// </summary>
	public sealed class SideCarSection {
		public string Name { get; }
		public int Weight { get; }
		private readonly IDictionary<string, SideCarProperty> _properties = new Dictionary<string, SideCarProperty>();
		/// <summary>
		/// Gets all properties in the section
		/// </summary>
		public IEnumerable<SideCarProperty> Properties => _properties.Values;
		/// <summary>
		/// Creates a new SideCar section
		/// weight sorts ascending order, and then by ascending name.
		/// But negative weight, is sorted last and only by name.
		/// </summary>
		/// <param name="name">Name of the section</param>
		/// <param name="weight">Weight for sorting (default: -1)</param>
		public SideCarSection(string name, int weight = -1) {
			ArgumentException.ThrowIfNullOrWhiteSpace(name);

			Name = name;
			Weight = weight;
		}

		/// <summary>
		/// Adds a property to the section.
		/// Overwrites property, if key already exists.
		/// </summary>
		/// <param name="property">Property to add</param>
		public void AddProperty(SideCarProperty property) {
			ArgumentNullException.ThrowIfNull(property);
			_properties[property.Key] = property;
		}
		/// <summary>
		/// Gets existing property or creates new one if it doesn't exist
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="value">Property value</param>
		/// <param name="weight">Weight for sorting if creating new property (default: -1)</param>
		/// <returns>Existing or newly created property</returns>
		public SideCarProperty GetOrCreateProperty(string name, string? value = null, int weight = -1) {
			if(_properties.TryGetValue(name, out var existingProperty)) {
				return existingProperty;
			}

			var newProperty = SideCarProperty.From(name, value, weight);
			_properties[name] = newProperty;
			return newProperty;
		}
		/// <summary>
		/// Adds a property with string value using fluent API
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="value">Property value</param>
		/// <param name="weight">Weight for sorting (default: -1)</param>
		/// <returns>This section for fluent chaining</returns>
		public SideCarSection WithProperty(string name, string? value, int weight = -1) {
			AddProperty(SideCarProperty.From(name, value, weight));
			return this;
		}

		/// <summary>
		/// Adds a property with DateTime value using fluent API
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="value">DateTime value</param>
		/// <param name="weight">Weight for sorting (default: -1)</param>
		/// <returns>This section for fluent chaining</returns>
		public SideCarSection WithProperty(string name, DateTime? value, int weight = -1) {
			AddProperty(SideCarProperty.From(name, value, weight));
			return this;
		}

		/// <summary>
		/// Adds a property with numeric value using fluent API
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="value">Numeric value</param>
		/// <param name="weight">Weight for sorting (default: -1)</param>
		/// <returns>This section for fluent chaining</returns>
		public SideCarSection WithProperty(string name, long? value, int weight = -1) {
			AddProperty(SideCarProperty.From(name, value, weight));
			return this;
		}
		/// <summary>
		/// Adds a property with boolean value using fluent API
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="value">Boolean value</param>
		/// <param name="weight">Weight for sorting (default: -1)</param>
		/// <returns>This section for fluent chaining</returns>
		public SideCarSection WithProperty(string name, bool? value, int weight = -1) {
			AddProperty(SideCarProperty.From(name, value, weight));
			return this;
		}
		/// <summary>
		/// Returns properties so that:
		/// 1) All with Weight gte 0 come first (ordered by Weight asc, then Name)
		/// 2) All with Weight lt 0 come last (ordered only by Name)
		/// </summary>
		public IEnumerable<SideCarProperty> GetSortedProperties() {
			return Properties
				.OrderBy(p => p.Weight < 0)                    // Non-negative first (false), negatives last (true)
				.ThenBy(p => p.Weight < 0 ? 0 : p.Weight)      // Only differentiates non-negative group
				.ThenBy(p => p.Key, StringComparer.Ordinal);   // Key ordering (tie-breaker; only key for negatives)
		}
	}
}
