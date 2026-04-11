using BMTP3.Core2.BackupNew.Api.Request.Enums;
using BMTP3.Core2.BackupNew.Engine.Strategies;
using Microsoft.Extensions.Logging.Abstractions;

namespace BMTP3.Core2.Tests.Strategies;

public class SidecarGeneratorFactoryTests
{
	// -----------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------

	/// <summary>
	///     Builds a factory pre-populated with real Ini and Json generators.
	/// </summary>
	private static SidecarGeneratorFactory BuildFactory()
	{
		Dictionary<SidecarFormat, ISidecarGenerator> generators = new()
		{
			[SidecarFormat.Ini] = new IniSidecarGenerator(new NullLogger<IniSidecarGenerator>()),
			[SidecarFormat.Json] = new JsonSidecarGenerator(new NullLogger<JsonSidecarGenerator>())
		};

		return new SidecarGeneratorFactory(generators);
	}

	// -----------------------------------------------------------------
	// 1. Create_Ini_ReturnsIniSidecarGenerator
	// -----------------------------------------------------------------

	[Fact]
	public void Create_Ini_ReturnsIniSidecarGenerator()
	{
		SidecarGeneratorFactory factory = BuildFactory();

		ISidecarGenerator? generator = factory.Create(SidecarFormat.Ini);

		Assert.NotNull(generator);
		Assert.IsType<IniSidecarGenerator>(generator);
	}

	// -----------------------------------------------------------------
	// 2. Create_Json_ReturnsJsonSidecarGenerator
	// -----------------------------------------------------------------

	[Fact]
	public void Create_Json_ReturnsJsonSidecarGenerator()
	{
		SidecarGeneratorFactory factory = BuildFactory();

		ISidecarGenerator? generator = factory.Create(SidecarFormat.Json);

		Assert.NotNull(generator);
		Assert.IsType<JsonSidecarGenerator>(generator);
	}

	// -----------------------------------------------------------------
	// 3. Create_None_ReturnsNull
	//    SidecarGeneratorFactory.Create() does a dictionary TryGetValue –
	//    SidecarFormat.None was not registered so it returns null.
	// -----------------------------------------------------------------

	[Fact]
	public void Create_None_ReturnsNull()
	{
		SidecarGeneratorFactory factory = BuildFactory();

		ISidecarGenerator? generator = factory.Create(SidecarFormat.None);

		Assert.Null(generator);
	}

	// -----------------------------------------------------------------
	// 4. Create_UnknownFormat_ReturnsNull
	//    An unregistered (cast) value not in the dictionary should also
	//    return null because TryGetValue silently misses the key.
	// -----------------------------------------------------------------

	[Fact]
	public void Create_UnknownFormat_ReturnsNull()
	{
		SidecarGeneratorFactory factory = BuildFactory();
		// Cast an integer outside the known enum range to SidecarFormat
		SidecarFormat unknownFormat = (SidecarFormat)999;

		ISidecarGenerator? generator = factory.Create(unknownFormat);

		Assert.Null(generator);
	}
}