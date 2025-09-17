using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3.Core.Metadata.SideCar {
    /// <summary>
    /// Represents a property (key-value pair) in a SideCar section
    /// </summary>
    /// <param name="Key">Property key</param>
    /// <param name="Value">Property value (can be null)</param>
    /// <param name="Weight">Weight for sorting (default: 100)</param>
    public sealed record SideCarProperty(string Key, string? Value, int Weight = 100) {
		public string Key { get; init; } = Key ?? throw new ArgumentNullException(nameof(Key));

		/// <summary>
		/// Creates a SideCarProperty from a DateTime value
		/// </summary>
		/// <param name="key">Property key</param>
		/// <param name="value">DateTime value (formatted with ToString("o"))</param>
		/// <param name="weight">Weight for sorting (default: 100)</param>
		/// <returns>New SideCarProperty instance</returns>
		public static SideCarProperty From(string key, DateTime? value, int weight = 100) {
			return new SideCarProperty(key, value?.ToString("o"), weight);
		}

		/// <summary>
		/// Creates a SideCarProperty from a long value
		/// </summary>
		/// <param name="key">Property key</param>
		/// <param name="value">Long value</param>
		/// <param name="weight">Weight for sorting (default: 100)</param>
		/// <returns>New SideCarProperty instance</returns>
		public static SideCarProperty From(string key, long? value, int weight = 100) {
			return new SideCarProperty(key, value?.ToString(), weight);
		}

		/// <summary>
		/// Creates a SideCarProperty from an integer value
		/// </summary>
		/// <param name="key">Property key</param>
		/// <param name="value">Integer value</param>
		/// <param name="weight">Weight for sorting (default: 100)</param>
		/// <returns>New SideCarProperty instance</returns>
		public static SideCarProperty From(string key, int? value, int weight = 100) {
			return new SideCarProperty(key, value?.ToString(), weight);
		}

		/// <summary>
		/// Creates a SideCarProperty from a boolean value
		/// </summary>
		/// <param name="key">Property key</param>
		/// <param name="value">Boolean value</param>
		/// <param name="weight">Weight for sorting (default: 100)</param>
		/// <returns>New SideCarProperty instance</returns>
		public static SideCarProperty From(string key, bool? value, int weight = 100) {
			return new SideCarProperty(key, value?.ToString().ToLowerInvariant(), weight);
		}

		/// <summary>
		/// Creates a SideCarProperty from a string value
		/// </summary>
		/// <param name="key">Property key</param>
		/// <param name="value">String value</param>
		/// <param name="weight">Weight for sorting (default: 100)</param>
		/// <returns>New SideCarProperty instance</returns>
		public static SideCarProperty From(string key, string? value, int weight = 100) {
			return new SideCarProperty(key, value, weight);
		}
	}
}
