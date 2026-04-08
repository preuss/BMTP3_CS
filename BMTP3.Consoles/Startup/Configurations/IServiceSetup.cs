using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BMTP3.Consoles.Startup.Configurations;

public interface IServiceSetup
{
	void Configure(IServiceCollection services, IConfiguration configuration);
}