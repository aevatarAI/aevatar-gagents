using Aevatar.GAgents.TestBase;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.WebCrawler.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule)
)]
public class AevatarWebCrawlerTestModule : AbpModule
{
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarWebCrawlerTestModule>(); });
        context.Services.AddSingleton(new ApplicationPartManager());
        
        var configuration = context.Services.GetConfiguration();
       
    }
}