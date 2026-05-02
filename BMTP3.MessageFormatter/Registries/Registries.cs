namespace BMTP3.MessageFormatter.Registries
{
	using Abstractions;

	/// <summary>
	/// Registry for runtime types (number, date, datetime, time).
	/// </summary>
	public class FormatTypeRegistry : IFormatTypeRegistry
	{
		private readonly Dictionary<string, IFormatType> _types = new(StringComparer.OrdinalIgnoreCase);

		public void Register(IFormatType formatType)
		{
			if (formatType == null)
				throw new ArgumentNullException(nameof(formatType));
			_types[formatType.Name] = formatType;
		}

		public IFormatType GetFormatType(string name)
		{
			if (TryGetFormatType(name, out IFormatType? type))
				return type!;
			throw new InvalidOperationException($"FormatType '{name}' not registered");
		}

		public bool TryGetFormatType(string name, out IFormatType? formatType)
		{
			return _types.TryGetValue(name, out formatType);
		}
	}

	/// <summary>
	/// Registry for functions by type.
	/// </summary>
	public class FunctionRegistry : IFunctionRegistry
	{
		private readonly Dictionary<string, Dictionary<string, IFunction>> _functions
			= new(StringComparer.OrdinalIgnoreCase);

		public void Register(string typeName, IFunction function)
		{
			if (string.IsNullOrEmpty(typeName))
				throw new ArgumentNullException(nameof(typeName));
			if (function == null)
				throw new ArgumentNullException(nameof(function));

			if (!_functions.ContainsKey(typeName))
				_functions[typeName] = new Dictionary<string, IFunction>(StringComparer.OrdinalIgnoreCase);

			_functions[typeName][function.Name] = function;
		}

		public IFunction GetFunction(string typeName, string functionName)
		{
			if (TryGetFunction(typeName, functionName, out IFunction? function))
				return function!;
			throw new FunctionNotRegisteredException(functionName, typeName);
		}

		public bool TryGetFunction(string typeName, string functionName, out IFunction? function)
		{
			function = null;

			if (!_functions.TryGetValue(typeName, out Dictionary<string, IFunction>? typeFunc))
				return false;

			return typeFunc.TryGetValue(functionName, out function);
		}
	}
}
