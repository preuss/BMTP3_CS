namespace BMTP3.MessageFormatter.Types
{
	using Abstractions;
	using System.Globalization;
	using System.Text;

	/// <summary>
	/// FormatType for numeric values (integer, float, double, decimal).
	/// Supports FormatStyles: integer, currency, percent, thousands, scientific.
	/// Supports CustomPatterns: #,##0.00 style patterns.
	/// </summary>
	public class NumberFormatType : IFormatType
	{
		public string Name => "number";

		public void AssertCompatible(object? value)
		{
			if (value == null)
				return;

			if (!(value is int || value is long || value is double || value is float || value is decimal))
				throw new FormatTypeAssertionException("number", value.GetType().Name);
		}

		public string FormatDefault(object? value)
		{
			if (value == null)
				return string.Empty;

			return value?.ToString() ?? string.Empty;
		}

		public string FormatWithStyle(object? value, string styleName)
		{
			AssertCompatible(value);

			return styleName.ToLowerInvariant() switch
			{
				"integer" => FormatAsInteger(value),
				"currency" => FormatAsCurrency(value),
				"percent" => FormatAsPercent(value),
				"thousands" => FormatWithThousands(value),
				"scientific" => FormatAsScientific(value),
				_ => throw new FormatStyleNotRegisteredException(styleName, Name)
			};
		}

		public string FormatWithPattern(object? value, string pattern)
		{
			AssertCompatible(value);
			return FormatWithCustomPattern(value, pattern);
		}

		public IEnumerable<string> GetAvailableStyles()
		{
			return new[] { "integer", "currency", "percent", "thousands", "scientific" };
		}

		public bool IsStyleAvailable(string styleName)
		{
			return GetAvailableStyles().Contains(styleName.ToLowerInvariant());
		}

		private string FormatAsInteger(object? value)
		{
			double num = Convert.ToDouble(value);
			// Half-even (banker's) rounding
			return Math.Round(num, MidpointRounding.ToEven).ToString("F0", CultureInfo.InvariantCulture);
		}

		private string FormatAsCurrency(object? value)
		{
			double num = Convert.ToDouble(value);
			return num.ToString("C2", CultureInfo.CurrentCulture);
		}

		private string FormatAsPercent(object? value)
		{
			double num = Convert.ToDouble(value);
			return (num * 100).ToString("F2", CultureInfo.InvariantCulture) + "%";
		}

		private string FormatWithThousands(object? value)
		{
			double num = Convert.ToDouble(value);
			return num.ToString("#,##0.##", CultureInfo.InvariantCulture);
		}

		private string FormatAsScientific(object? value)
		{
			double num = Convert.ToDouble(value);
			return num.ToString("E4", CultureInfo.InvariantCulture);
		}

		private string FormatWithCustomPattern(object? value, string pattern)
		{
			// Handle escape sequences first: }} -> } and {{ -> {
			pattern = pattern.Replace("}}", "§RBRACE§").Replace("{{", "§LBRACE§");

			double num = Convert.ToDouble(value);

			// Simple custom pattern support: #,##0.00 style patterns
			// This is a simplified implementation - a full one would parse all tokens

			// Handle positive;negative sections
			int semiPos = pattern.IndexOf(';');
			if (semiPos > 0)
			{
				string positivePattern = pattern[..semiPos];
				string negativePattern = pattern[(semiPos + 1)..];

				if (num < 0)
				{
					string result = FormatNumberWithSimplePattern(Math.Abs(num), negativePattern).Replace("(", "(").Replace(")", ")");
					result = result.Replace("§RBRACE§", "}").Replace("§LBRACE§", "{");
					return result;
				}

				string positiveResult = FormatNumberWithSimplePattern(num, positivePattern);
				positiveResult = positiveResult.Replace("§RBRACE§", "}").Replace("§LBRACE§", "{");
				return positiveResult;
			}

			string output = FormatNumberWithSimplePattern(num, pattern);
			output = output.Replace("§RBRACE§", "}").Replace("§LBRACE§", "{");
			return output;
		}

		private string FormatNumberWithSimplePattern(double num, string pattern)
		{
			// Extract literal text and number format
			// For simplicity, we'll handle basic patterns like: #,##0.00, 0.00, Total: #,##0.00 kr.
			
			StringBuilder result = new();
			StringBuilder numberFormat = new();
			bool inNumber = false;

			foreach (char c in pattern)
			{
				if (c == '#' || c == '0' || c == ',' || c == '.')
				{
					numberFormat.Append(c);
					inNumber = true;
				}
				else
				{
					if (inNumber && numberFormat.Length > 0)
					{
						result.Append(FormatNumberAccordingToFormat(num, numberFormat.ToString()));
						numberFormat.Clear();
						inNumber = false;
					}
					result.Append(c);
				}
			}

			if (numberFormat.Length > 0)
			{
				result.Append(FormatNumberAccordingToFormat(num, numberFormat.ToString()));
			}

			return result.ToString();
		}

		private string FormatNumberAccordingToFormat(double num, string format)
		{
			// Simplified: handle common formats
			if (format.Contains('.'))
			{
				int decimals = format.Length - format.IndexOf('.') - 1;
				return num.ToString($"F{decimals}", CultureInfo.InvariantCulture);
			}

			if (format.Contains(','))
			{
				return num.ToString("#,##0", CultureInfo.InvariantCulture);
			}

			return num.ToString("F0", CultureInfo.InvariantCulture);
		}
	}
}
