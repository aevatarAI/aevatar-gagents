using AevatarGAgents.MicroAI.Options;
using AevatarGAgents.NamingContest.Common;
using AevatarGAgents.NamingContest.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AevatarGAgents.NamingContest;

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