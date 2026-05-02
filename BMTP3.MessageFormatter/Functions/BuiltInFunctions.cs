namespace BMTP3.MessageFormatter.Functions
{
	using Abstractions;

	/// <summary>
	/// Built-in string functions.
	/// </summary>
	public class TrimFunction : IFunction
	{
		public string Name => "trim";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			return (value?.ToString() ?? string.Empty).Trim();
		}

		public string GetResultType(string inputType) => "string";
	}

	public class ToUpperFunction : IFunction
	{
		public string Name => "toUpper";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			return (value?.ToString() ?? string.Empty).ToUpperInvariant();
		}

		public string GetResultType(string inputType) => "string";
	}

	public class ToLowerFunction : IFunction
	{
		public string Name => "toLower";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			return (value?.ToString() ?? string.Empty).ToLowerInvariant();
		}

		public string GetResultType(string inputType) => "string";
	}

	public class SubstringFunction : IFunction
	{
		public string Name => "substring";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			string str = value.ToString() ?? string.Empty;

			if (arguments.Count < 1)
				throw new ArgumentException("substring requires at least 1 argument (start index)");

			int start = Convert.ToInt32(arguments[0]);

			if (arguments.Count >= 2)
			{
				int length = Convert.ToInt32(arguments[1]);
				if (start < 0 || start >= str.Length)
					return string.Empty;
				if (start + length > str.Length)
					length = str.Length - start;
				return str.Substring(start, length);
			}

			return str[start..];
		}

		public string GetResultType(string inputType) => "string";
	}

	public class ReplaceFunction : IFunction
	{
		public string Name => "replace";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			if (arguments.Count < 2)
				throw new ArgumentException("replace requires 2 arguments (oldValue, newValue)");

			string str = value.ToString() ?? string.Empty;
			string oldValue = arguments[0]?.ToString() ?? string.Empty;
			string newValue = arguments[1]?.ToString() ?? string.Empty;

			return str.Replace(oldValue, newValue);
		}

		public string GetResultType(string inputType) => "string";
	}

	public class PadLeftFunction : IFunction
	{
		public string Name => "padLeft";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			if (arguments.Count < 1)
				throw new ArgumentException("padLeft requires 1 argument (width)");

			string str = value.ToString() ?? string.Empty;
			int width = Convert.ToInt32(arguments[0]);
			char padChar = ' ';

			if (arguments.Count >= 2)
				padChar = arguments[1]?.ToString()?[0] ?? ' ';

			return str.PadLeft(width, padChar);
		}

		public string GetResultType(string inputType) => "string";
	}

	public class PadRightFunction : IFunction
	{
		public string Name => "padRight";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			if (arguments.Count < 1)
				throw new ArgumentException("padRight requires 1 argument (width)");

			string str = value.ToString() ?? string.Empty;
			int width = Convert.ToInt32(arguments[0]);
			char padChar = ' ';

			if (arguments.Count >= 2)
				padChar = arguments[1]?.ToString()?[0] ?? ' ';

			return str.PadRight(width, padChar);
		}

		public string GetResultType(string inputType) => "string";
	}

	/// <summary>
	/// Built-in numeric functions.
	/// </summary>
	public class AbsFunction : IFunction
	{
		public string Name => "abs";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			if (value is int intVal)
				return Math.Abs(intVal);
			if (value is long longVal)
				return Math.Abs(longVal);
			if (value is double doubleVal)
				return Math.Abs(doubleVal);
			if (value is decimal decimalVal)
				return Math.Abs(decimalVal);
			if (value is float floatVal)
				return Math.Abs(floatVal);

			throw new InvalidOperationException($"abs() not supported for type {value.GetType().Name}");
		}

		public string GetResultType(string inputType) => inputType;
	}

	public class ToStringFunction : IFunction
	{
		public string Name => "toString";

		public object? Execute(object? value, List<object> arguments)
		{
			return value?.ToString() ?? string.Empty;
		}

		public string GetResultType(string inputType) => "string";
	}

	/// <summary>
	/// Built-in DateTime functions.
	/// </summary>
	public class AddDaysFunction : IFunction
	{
		public string Name => "addDays";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			if (arguments.Count < 1)
				throw new ArgumentException("addDays requires 1 argument (days)");

			int days = Convert.ToInt32(arguments[0]);

			if (value is DateTime dt)
				return dt.AddDays(days);

			throw new InvalidOperationException($"addDays() not supported for type {value.GetType().Name}");
		}

		public string GetResultType(string inputType) => inputType;
	}

	public class AddHoursFunction : IFunction
	{
		public string Name => "addHours";

		public object? Execute(object? value, List<object> arguments)
		{
			if (value == null)
				return null;

			if (arguments.Count < 1)
				throw new ArgumentException("addHours requires 1 argument (hours)");

			int hours = Convert.ToInt32(arguments[0]);

			if (value is DateTime dt)
				return dt.AddHours(hours);

			throw new InvalidOperationException($"addHours() not supported for type {value.GetType().Name}");
		}

		public string GetResultType(string inputType) => inputType;
	}
}
