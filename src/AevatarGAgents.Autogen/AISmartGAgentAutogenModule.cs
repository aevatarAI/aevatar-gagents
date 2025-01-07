using AevatarGAgents.Autogen.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AevatarGAgents.Autogen;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AISmartGAgentAutogenModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<AutogenOptions>(configuration.GetSection("AutogenConfig"));
        //
        // var autogenConfig = context.Services.GetRequiredService<IOptions<AutogenOptions>>().Value;
        // context.Services.AddTransient<ChatClient>(op => new ChatClient(autogenConfig.Model, autogenConfig.ApiKey));
        // context.Services.AddTransient<IChatAgentProvider, ChatAgentProvider>();
        context.Services.AddTransient<IChatAgentProvider, SemanticProvider>();
        // context.Services.AddTransient<IChatService, ChatService>();
        // Configure<RagOptions>(configuration.GetSection("AutogenConfig:AutoGenRag"));
        Configure<SemanticOptions>(configuration.GetSection("AutogenConfig:Semantic"));
    }
}