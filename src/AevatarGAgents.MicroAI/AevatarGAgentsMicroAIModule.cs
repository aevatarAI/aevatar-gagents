using AevatarGAgents.MicroAI.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AevatarGAgents.MicroAI;

[DependsOn(
    typeof(AbpAutoMapperModule)
    )]
public class AevatarGAgentsMicroAIModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentsMicroAIModule>();
        });
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<MicroAIOptions>(configuration.GetSection("AutogenConfig"));
    }
}
