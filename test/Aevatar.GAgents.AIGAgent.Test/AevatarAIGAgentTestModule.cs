using Aevatar.GAgents.AIGAgent.Test.Modules;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Aws;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.AIGAgent.Test;


[DependsOn(typeof(AevatarGAgentTestBaseModule),
    typeof(AbpBlobStoringModule),
    typeof(MockBrainTestModule)
    )]
public class AevatarAIGAgentTestModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlobContainer, MockBlobContainer>();
    }
}