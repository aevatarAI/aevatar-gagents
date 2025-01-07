
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;


namespace AISmart;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AISmartCQRSModule)

)]
public class AISmartOrleansTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = false;
        });
        context.Services.AddSingleton<ClusterFixture>();
        context.Services.AddSingleton<IClusterClient>(sp => context.Services.GetRequiredService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<IGrainFactory>(sp => context.Services.GetRequiredService<ClusterFixture>().Cluster.GrainFactory);
        context.Services.AddSingleton<ICQRSProvider, CQRSProvider>();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AISmartApplicationModule>(); });

    }
}
