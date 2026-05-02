namespace BMTP3.MessageFormatter.Types
{
	using Abstractions;
	using System.Globalization;
	using System.Text;

	/// <summary>
	/// FormatType for date values.
	/// Supports FormatStyles: short, medium, long, full, iso.
	/// Supports CustomPatterns: YYYY-MM-DD style patterns.
	/// </summary>
	public class DateFormatType : IFormatType
	{
		public string Name => "date";

		public void AssertCompatible(object? value)
		{
			if (value == null)
				return;

			if (!(value is DateTime || value is DateOnly))
				throw new FormatTypeAssertionException("date", value.GetType().Name);

			// If DateTime, ensure it's a date (no time component)
			if (value is DateTime dt && dt.TimeOfDay != TimeSpan.Zero)
				throw new FormatTypeAssertionException("date", "datetime (with time component)");
		}

		public string FormatDefault(object? value)
		{
			if (value == null)
				return string.Empty;

			DateTime date = GetDateTime(value);
			return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		public string FormatWithStyle(object? value, string styleName)
		{
			AssertCompatible(value);

			return styleName.ToLowerInvariant() switch
			{
				"short" => FormatShort(GetDateTime(value)),
				"medium" => FormatMedium(GetDateTime(value)),
				"long" => FormatLong(GetDateTime(value)),
				"full" => FormatFull(GetDateTime(value)),
				"iso" => FormatISO(GetDateTime(value)),
				_ => throw new FormatStyleNotRegisteredException(styleName, Name)
			};
		}

		public string FormatWithPattern(object? value, string pattern)
		{
			AssertCompatible(value);
			return FormatWithCustomPattern(GetDateTime(value), pattern);
		}

		public IEnumerable<string> GetAvailableStyles()
		{
			return new[] { "short", "medium", "long", "full", "iso" };
		}

		public bool IsStyleAvailable(string styleName)
		{
			return GetAvailableStyles().Contains(styleName.ToLowerInvariant());
		}

		private string FormatShort(DateTime date)
		{
			return date.ToString("d", CultureInfo.CurrentCulture);
		}

		private string FormatMedium(DateTime date)
		{
			return date.ToString("MMM dd yyyy", CultureInfo.CurrentCulture);
		}

		private string FormatLong(DateTime date)
		{
			return date.ToString("MMMM dd yyyy", CultureInfo.CurrentCulture);
		}

		private string FormatFull(DateTime date)
		{
			return date.ToString("dddd MMMM dd yyyy", CultureInfo.CurrentCulture);
		}

		private string FormatISO(DateTime date)
		{
			return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		private string FormatWithCustomPattern(DateTime date, string pattern)
		{
			// Handle escape sequences first: }} -> } and {{ -> {
			pattern = pattern.Replace("}}", "§RBRACE§").Replace("{{", "§LBRACE§");

			// Parse and apply custom pattern tokens: YYYY, YY, MM, M, DD, D
			StringBuilder result = new();
			int i = 0;

			while (i < pattern.Length)
			{
				if (pattern[i..].StartsWith("YYYY"))
				{
					result.Append(date.Year);
					i += 4;
				}
				else if (pattern[i..].StartsWith("YY"))
				{
					result.Append(date.Year % 100);
					i += 2;
				}
				else if (pattern[i..].StartsWith("MM"))
				{
					result.Append(date.Month.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 'M')
				{
					result.Append(date.Month);
					i++;
				}
				else if (pattern[i..].StartsWith("DD"))
				{
					result.Append(date.Day.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 'D')
				{
					result.Append(date.Day);
					i++;
				}
				else
				{
					result.Append(pattern[i]);
					i++;
				}
			}

			// Restore escape sequences
			string output = result.ToString();
			output = output.Replace("§RBRACE§", "}").Replace("§LBRACE§", "{");
			return output;
		}

		private DateTime GetDateTime(object? value)
		{
			if (value is DateTime dt)
				return dt;
			if (value is DateOnly dos)
				return dos.ToDateTime(TimeOnly.MinValue);
			throw new InvalidOperationException($"Cannot convert {value?.GetType().Name} to DateTime");
		}
	}

	/// <summary>
	/// FormatType for datetime values (date + time).
	/// Supports FormatStyles: short, medium, long, full, iso.
	/// Supports CustomPatterns: YYYY-MM-DD hh:mm:ss style patterns.
	/// </summary>
	public class DateTimeFormatType : IFormatType
	{
		public string Name => "datetime";

		public void AssertCompatible(object? value)
		{
			if (value == null)
				return;

			if (!(value is DateTime))
				throw new FormatTypeAssertionException("datetime", value.GetType().Name);
		}

		public string FormatDefault(object? value)
		{
			if (value == null)
				return string.Empty;

			return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		public string FormatWithStyle(object? value, string styleName)
		{
			AssertCompatible(value);
			DateTime dt = (DateTime)value!;

			return styleName.ToLowerInvariant() switch
			{
				"short" => FormatShort(dt),
				"medium" => FormatMedium(dt),
				"long" => FormatLong(dt),
				"full" => FormatFull(dt),
				"iso" => FormatISO(dt),
				_ => throw new FormatStyleNotRegisteredException(styleName, Name)
			};
		}

		public string FormatWithPattern(object? value, string pattern)
		{
			AssertCompatible(value);
			return FormatWithCustomPattern((DateTime)value!, pattern);
		}

		public IEnumerable<string> GetAvailableStyles()
		{
			return new[] { "short", "medium", "long", "full", "iso" };
		}

		public bool IsStyleAvailable(string styleName)
		{
			return GetAvailableStyles().Contains(styleName.ToLowerInvariant());
		}

		private string FormatShort(DateTime dt)
		{
			return dt.ToString("g", CultureInfo.CurrentCulture);
		}

		private string FormatMedium(DateTime dt)
		{
			return dt.ToString("MMM dd yyyy HH:mm:ss", CultureInfo.CurrentCulture);
		}

		private string FormatLong(DateTime dt)
		{
			return dt.ToString("MMMM dd yyyy HH:mm:ss zzz", CultureInfo.CurrentCulture);
		}

		private string FormatFull(DateTime dt)
		{
			return dt.ToString("dddd MMMM dd yyyy HH:mm:ss zzzz", CultureInfo.CurrentCulture);
		}

		private string FormatISO(DateTime dt)
		{
			return dt.ToString("O", CultureInfo.InvariantCulture).Replace("T", "T")[..19]; // Simplify: remove fractional seconds
		}

		private string FormatWithCustomPattern(DateTime dt, string pattern)
		{
			// Handle escape sequences first: }} -> } and {{ -> {
			pattern = pattern.Replace("}}", "§RBRACE§").Replace("{{", "§LBRACE§");

			// Parse and apply custom pattern tokens
			StringBuilder result = new();
			int i = 0;

			while (i < pattern.Length)
			{
				if (pattern[i..].StartsWith("YYYY"))
				{
					result.Append(dt.Year);
					i += 4;
				}
				else if (pattern[i..].StartsWith("YY"))
				{
					result.Append(dt.Year % 100);
					i += 2;
				}
				else if (pattern[i..].StartsWith("MM") && !IsTimeContext(pattern, i))
				{
					result.Append(dt.Month.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 'M' && !IsTimeContext(pattern, i))
				{
					result.Append(dt.Month);
					i++;
				}
				else if (pattern[i..].StartsWith("DD") && !IsTimeContext(pattern, i))
				{
					result.Append(dt.Day.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 'D' && !IsTimeContext(pattern, i))
				{
					result.Append(dt.Day);
					i++;
				}
				else if (pattern[i..].StartsWith("hh"))
				{
					result.Append(dt.Hour.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 'h')
				{
					result.Append(dt.Hour);
					i++;
				}
				else if (pattern[i..].StartsWith("mm"))
				{
					result.Append(dt.Minute.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 'm')
				{
					result.Append(dt.Minute);
					i++;
				}
				else if (pattern[i..].StartsWith("ss"))
				{
					result.Append(dt.Second.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 's')
				{
					result.Append(dt.Second);
					i++;
				}
				else if (pattern[i] == 'f')
				{
					// Fractional seconds: count consecutive 'f' characters
					int count = 0;
					int j = i;
					while (j < pattern.Length && pattern[j] == 'f')
					{
						count++;
						j++;
					}
					string fractional = (dt.Millisecond * 10000 + dt.Microsecond).ToString().PadRight(count, '0')[..count];
					result.Append(fractional);
					i = j;
				}
				else
				{
					result.Append(pattern[i]);
					i++;
				}
			}

			// Restore escape sequences
			string output = result.ToString();
			output = output.Replace("§RBRACE§", "}").Replace("§LBRACE§", "{");
			return output;
		}

		private bool IsTimeContext(string pattern, int pos)
		{
			// Check if we're in a time context (after 'h', 'm', 's', or 'f')
			for (int i = pos - 1; i >= 0; i--)
			{
				if (pattern[i] == 'h' || pattern[i] == 'm' || pattern[i] == 's' || pattern[i] == 'f')
					return true;
				if (pattern[i] == ' ' || pattern[i] == ':' || pattern[i] == '-' || pattern[i] == '/')
					continue;
				return false;
			}
			return false;
		}
	}

	/// <summary>
	/// FormatType for time values.
	/// Supports FormatStyles: short, medium, long, full.
	/// Supports CustomPatterns: hh:mm:ss style patterns.
	/// </summary>
	public class TimeFormatType : IFormatType
	{
		public string Name => "time";

		public void AssertCompatible(object? value)
		{
			if (value == null)
				return;

			if (!(value is TimeOnly || value is DateTime))
				throw new FormatTypeAssertionException("time", value.GetType().Name);
		}

		public string FormatDefault(object? value)
		{
			if (value == null)
				return string.Empty;

			TimeOnly time = GetTimeOnly(value);
			return time.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
		}

		public string FormatWithStyle(object? value, string styleName)
		{
			AssertCompatible(value);
			TimeOnly time = GetTimeOnly(value);

			return styleName.ToLowerInvariant() switch
			{
				"short" => FormatShort(time),
				"medium" => FormatMedium(time),
				"long" => FormatLong(time),
				"full" => FormatFull(time),
				_ => throw new FormatStyleNotRegisteredException(styleName, Name)
			};
		}

		public string FormatWithPattern(object? value, string pattern)
		{
			AssertCompatible(value);
			return FormatWithCustomPattern(GetTimeOnly(value), pattern);
		}

		public IEnumerable<string> GetAvailableStyles()
		{
			return new[] { "short", "medium", "long", "full" };
		}

		public bool IsStyleAvailable(string styleName)
		{
			return GetAvailableStyles().Contains(styleName.ToLowerInvariant());
		}

		private string FormatShort(TimeOnly time)
		{
			return time.ToString("HH:mm", CultureInfo.CurrentCulture);
		}

		private string FormatMedium(TimeOnly time)
		{
			return time.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
		}

		private string FormatLong(TimeOnly time)
		{
			return time.ToString("HH:mm:ss zzz", CultureInfo.CurrentCulture);
		}

		private string FormatFull(TimeOnly time)
		{
			return time.ToString("HH:mm:ss zzzz", CultureInfo.CurrentCulture);
		}

		private string FormatWithCustomPattern(TimeOnly time, string pattern)
		{
			// Handle escape sequences first: }} -> } and {{ -> {
			pattern = pattern.Replace("}}", "§RBRACE§").Replace("{{", "§LBRACE§");

			StringBuilder result = new();
			int i = 0;

			while (i < pattern.Length)
			{
				if (pattern[i..].StartsWith("hh"))
				{
					result.Append(time.Hour.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 'h')
				{
					result.Append(time.Hour);
					i++;
				}
				else if (pattern[i..].StartsWith("mm"))
				{
					result.Append(time.Minute.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 'm')
				{
					result.Append(time.Minute);
					i++;
				}
				else if (pattern[i..].StartsWith("ss"))
				{
					result.Append(time.Second.ToString("D2"));
					i += 2;
				}
				else if (pattern[i] == 's')
				{
					result.Append(time.Second);
					i++;
				}
				else if (pattern[i] == 'f')
				{
					int count = 0;
					int j = i;
					while (j < pattern.Length && pattern[j] == 'f')
					{
						count++;
						j++;
					}
					result.Append(time.Microsecond.ToString().PadRight(count, '0')[..Math.Min(count, 7)]);
					i = j;
				}
				else
				{
					result.Append(pattern[i]);
					i++;
				}
			}

			// Restore escape sequences
			string output = result.ToString();
			output = output.Replace("§RBRACE§", "}").Replace("§LBRACE§", "{");
			return output;
		}

		private TimeOnly GetTimeOnly(object? value)
		{
			if (value is TimeOnly to)
				return to;
			if (value is DateTime dt)
				return TimeOnly.FromDateTime(dt);
			throw new InvalidOperationException($"Cannot convert {value?.GetType().Name} to TimeOnly");
		}
	}
}
