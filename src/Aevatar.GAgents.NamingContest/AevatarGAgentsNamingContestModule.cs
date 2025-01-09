using Aevatar.GAgents.MicroAI.Options;
using Aevatar.GAgents.NamingContest.Common;
using Aevatar.GAgents.NamingContest.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.NamingContest;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsNamingContestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentsNamingContestModule>();
        });
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<MicroAIOptions>(configuration.GetSection("AutogenConfig"));
    }
}