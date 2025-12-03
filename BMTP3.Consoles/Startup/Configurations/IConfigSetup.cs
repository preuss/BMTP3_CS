using Microsoft.Extensions.Configuration;

namespace BMTP3.Consoles.Startup.Configurations;
public interface IConfigSetup
{
	void Configure(IConfigurationManager builder);
}
