namespace BMTP3.MessageFormatter.Tests.Registries
{
	using Xunit;
	using BMTP3.MessageFormatter.Registries;
	using BMTP3.MessageFormatter.Types;
	using BMTP3.MessageFormatter.Functions;

	public class RegistriesTests
	{
		#region FormatTypeRegistry Tests

		[Fact]
		public void FormatTypeRegistry_Register_StoresFormatType()
		{
			FormatTypeRegistry registry = new();
			NumberFormatType numberType = new();

			registry.Register(numberType);

			var retrieved = registry.GetFormatType("number");
			Assert.NotNull(retrieved);
			Assert.Equal("number", retrieved.Name);
		}

		[Fact]
		public void FormatTypeRegistry_Register_Null_Throws()
		{
			FormatTypeRegistry registry = new();

			Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
		}

		[Fact]
		public void FormatTypeRegistry_GetFormatType_NotRegistered_Throws()
		{
			FormatTypeRegistry registry = new();

			Assert.Throws<InvalidOperationException>(() => registry.GetFormatType("unknown"));
		}

		[Fact]
		public void FormatTypeRegistry_TryGetFormatType_Success_ReturnsTrue()
		{
			FormatTypeRegistry registry = new();
			registry.Register(new NumberFormatType());

			bool found = registry.TryGetFormatType("number", out var type);

			Assert.True(found);
			Assert.NotNull(type);
		}

		[Fact]
		public void FormatTypeRegistry_TryGetFormatType_NotFound_ReturnsFalse()
		{
			FormatTypeRegistry registry = new();

			bool found = registry.TryGetFormatType("unknown", out var type);

			Assert.False(found);
			Assert.Null(type);
		}

		[Fact]
		public void FormatTypeRegistry_CaseInsensitive_FindsType()
		{
			FormatTypeRegistry registry = new();
			registry.Register(new NumberFormatType());

			var type1 = registry.GetFormatType("number");
			var type2 = registry.GetFormatType("NUMBER");
			var type3 = registry.GetFormatType("Number");

			Assert.Equal(type1.Name, type2.Name);
			Assert.Equal(type1.Name, type3.Name);
		}

		[Fact]
		public void FormatTypeRegistry_OverwriteExisting_Works()
		{
			FormatTypeRegistry registry = new();
			registry.Register(new NumberFormatType());

			var first = registry.GetFormatType("number");
			
			registry.Register(new NumberFormatType());
			var second = registry.GetFormatType("number");

			Assert.NotNull(first);
			Assert.NotNull(second);
		}

		[Fact]
		public void FormatTypeRegistry_MultipleTypes_StoresAll()
		{
			FormatTypeRegistry registry = new();
			registry.Register(new NumberFormatType());
			registry.Register(new DateFormatType());

			var numberType = registry.GetFormatType("number");
			var dateType = registry.GetFormatType("date");

			Assert.NotNull(numberType);
			Assert.NotNull(dateType);
			Assert.Equal("number", numberType.Name);
			Assert.Equal("date", dateType.Name);
		}

		#endregion

		#region FunctionRegistry Tests

		[Fact]
		public void FunctionRegistry_Register_StoresFunction()
		{
			FunctionRegistry registry = new();
			TrimFunction trimFunc = new();

			registry.Register("string", trimFunc);

			var retrieved = registry.GetFunction("string", "trim");
			Assert.NotNull(retrieved);
			Assert.Equal("trim", retrieved.Name);
		}

		[Fact]
		public void FunctionRegistry_Register_NullTypeName_Throws()
		{
			FunctionRegistry registry = new();

			Assert.Throws<ArgumentNullException>(() => registry.Register(null!, new TrimFunction()));
		}

		[Fact]
		public void FunctionRegistry_Register_NullFunction_Throws()
		{
			FunctionRegistry registry = new();

			Assert.Throws<ArgumentNullException>(() => registry.Register("string", null!));
		}

		[Fact]
		public void FunctionRegistry_GetFunction_NotFound_Throws()
		{
			FunctionRegistry registry = new();

			Assert.Throws<FunctionNotRegisteredException>(() => registry.GetFunction("string", "unknown"));
		}

		[Fact]
		public void FunctionRegistry_TryGetFunction_Success_ReturnsTrue()
		{
			FunctionRegistry registry = new();
			registry.Register("string", new TrimFunction());

			bool found = registry.TryGetFunction("string", "trim", out var func);

			Assert.True(found);
			Assert.NotNull(func);
		}

		[Fact]
		public void FunctionRegistry_TryGetFunction_NotFound_ReturnsFalse()
		{
			FunctionRegistry registry = new();

			bool found = registry.TryGetFunction("string", "unknown", out var func);

			Assert.False(found);
			Assert.Null(func);
		}

		[Fact]
		public void FunctionRegistry_CaseInsensitive_FindsFunction()
		{
			FunctionRegistry registry = new();
			registry.Register("string", new TrimFunction());

			var func1 = registry.GetFunction("string", "trim");
			var func2 = registry.GetFunction("string", "TRIM");
			var func3 = registry.GetFunction("STRING", "Trim");

			Assert.Equal(func1.Name, func2.Name);
			Assert.Equal(func1.Name, func3.Name);
		}

		[Fact]
		public void FunctionRegistry_MultipleFunctionsPerType_StoresAll()
		{
			FunctionRegistry registry = new();
			registry.Register("string", new TrimFunction());
			registry.Register("string", new ToUpperFunction());

			var trimFunc = registry.GetFunction("string", "trim");
			var upperFunc = registry.GetFunction("string", "toUpper");

			Assert.NotNull(trimFunc);
			Assert.NotNull(upperFunc);
			Assert.Equal("trim", trimFunc.Name);
			Assert.Equal("toUpper", upperFunc.Name);
		}

		[Fact]
		public void FunctionRegistry_MultipleTypes_StoresAll()
		{
			FunctionRegistry registry = new();
			registry.Register("string", new TrimFunction());
			registry.Register("number", new AbsFunction());

			var stringFunc = registry.GetFunction("string", "trim");
			var numberFunc = registry.GetFunction("number", "abs");

			Assert.NotNull(stringFunc);
			Assert.NotNull(numberFunc);
		}

		[Fact]
		public void FunctionRegistry_OverwriteExisting_Works()
		{
			FunctionRegistry registry = new();
			registry.Register("string", new TrimFunction());

			var first = registry.GetFunction("string", "trim");

			registry.Register("string", new TrimFunction());
			var second = registry.GetFunction("string", "trim");

			Assert.NotNull(first);
			Assert.NotNull(second);
		}

		[Fact]
		public void FunctionRegistry_TryGetFunction_WrongType_ReturnsFalse()
		{
			FunctionRegistry registry = new();
			registry.Register("string", new TrimFunction());

			bool found = registry.TryGetFunction("number", "trim", out var func);

			Assert.False(found);
			Assert.Null(func);
		}

		#endregion
	}
}
