using AevatarGAgents.AElf.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AevatarGAgents.AElf;

[DependsOn(typeof(AbpAutoMapperModule))]
public class AevatarGAgentAElfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentAElfModule>();
        });
        var configuration = context.Services.GetConfiguration();
        Configure<ChainOptions>(configuration.GetSection("Chain"));   
    }
}
