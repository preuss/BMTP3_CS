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
	}
}
