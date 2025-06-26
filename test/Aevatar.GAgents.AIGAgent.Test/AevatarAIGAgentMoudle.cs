using Aevatar.GAgents.AIGAgent.Test.Modules;
using Aevatar.GAgents.TestBase;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.AIGAgent.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule),
    typeof(MockBrainTestModule)
)]
public class AevatarAIGAgentMoudle: AbpModule
{
    
}