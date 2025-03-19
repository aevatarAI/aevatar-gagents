using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.WebCrawler;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsWebCrawlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentsWebCrawlerModule>();
        });
    }
}