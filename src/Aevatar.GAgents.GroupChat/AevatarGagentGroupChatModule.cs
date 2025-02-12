using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.GroupChat;


[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsGroupChatModule: AbpModule
{
    
}