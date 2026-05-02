namespace BMTP3.MessageFormatter
{
	using Abstractions;
	using Core;
	using Models;
	using Registries;
	using Functions;
	using Types;
	using System.Text;

	/// <summary>
	/// Main facade for message formatting with the MessageFormatter syntax v4.1.5.
	/// </summary>
	public class MessageFormatter : IMessageFormatter
	{
		private readonly IMessageParser _parser;
		private readonly IMessageEvaluator _evaluator;

		public MessageFormatter(IFormatTypeRegistry? formatTypeRegistry = null, IFunctionRegistry? functionRegistry = null)
		{
			formatTypeRegistry ??= CreateDefaultFormatTypeRegistry();
			functionRegistry ??= CreateDefaultFunctionRegistry();

			_parser = new MessageParser();
			_evaluator = new MessageEvaluator(formatTypeRegistry, functionRegistry);
		}

		public string Format(string template, Dictionary<string, object?> args)
		{
			if (string.IsNullOrEmpty(template))
				return template ?? string.Empty;

			System.Diagnostics.Debug.WriteLine($"[Format] Parsing template: {template}");
			List<object> parsed = _parser.Parse(template);
			System.Diagnostics.Debug.WriteLine($"[Format] Parsed {parsed.Count} segments");
			return EvaluateSegments(parsed, new EvaluationContext(args));
		}

		public string Format(string template, params object?[] args)
		{
			if (string.IsNullOrEmpty(template))
				return template ?? string.Empty;

			List<object> parsed = _parser.Parse(template);
			return EvaluateSegments(parsed, new EvaluationContext(args));
		}

		public string Format(string template, Dictionary<string, object?> namedArgs, params object?[] indexedArgs)
		{
			if (string.IsNullOrEmpty(template))
				return template ?? string.Empty;

			List<object> parsed = _parser.Parse(template);
			return EvaluateSegments(parsed, new EvaluationContext(namedArgs, indexedArgs));
		}

		public string Format(string template, EvaluationContext context)
		{
			if (string.IsNullOrEmpty(template))
				return template ?? string.Empty;

			List<object> parsed = _parser.Parse(template);
			return EvaluateSegments(parsed, context);
		}

		private string EvaluateSegments(List<object> segments, EvaluationContext context)
		{
			StringBuilder sb = new();

			foreach (object segment in segments)
			{
				if (segment is string text)
				{
					sb.Append(text);
				}
				else if (segment is ParsedExpression expr)
				{
					sb.Append(_evaluator.Evaluate(expr, context));
				}
			}

			return sb.ToString();
		}

		private IFormatTypeRegistry CreateDefaultFormatTypeRegistry()
		{
			FormatTypeRegistry registry = new();
			registry.Register(new NumberFormatType());
			registry.Register(new DateFormatType());
			registry.Register(new DateTimeFormatType());
			registry.Register(new TimeFormatType());
			return registry;
		}

		private IFunctionRegistry CreateDefaultFunctionRegistry()
		{
			FunctionRegistry registry = new();

			// String functions
			registry.Register("string", new TrimFunction());
			registry.Register("string", new ToUpperFunction());
			registry.Register("string", new ToLowerFunction());
			registry.Register("string", new SubstringFunction());
			registry.Register("string", new ReplaceFunction());
			registry.Register("string", new PadLeftFunction());
			registry.Register("string", new PadRightFunction());

			// Numeric functions
			registry.Register("integer", new AbsFunction());
			registry.Register("number", new AbsFunction());
			registry.Register("integer", new ToStringFunction());
			registry.Register("number", new ToStringFunction());

			// DateTime functions
			registry.Register("datetime", new AddDaysFunction());
			registry.Register("datetime", new AddHoursFunction());
			registry.Register("date", new AddDaysFunction());

			return registry;
		}
	}
}

