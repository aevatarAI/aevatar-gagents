using Aevatar.GAgents.Twitter.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Twitter;

[DependsOn(
    typeof(AbpAutoMapperModule)
    )]
public class AevatarGAgentsTwitterModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentsTwitterModule>();
        });
        var configuration = context.Services.GetConfiguration();
        Configure<TwitterOptions>(configuration.GetSection("Twitter")); 
    }
}
