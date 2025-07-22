using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;

[GenerateSerializer]
public class AIAgentStatusProxyConfig : ConfigurationBase
{
    [Id(0)]
    [DefaultValues("You are an AI agent status monitor responsible for tracking and reporting agent health")]
    public string Instructions { get; set; }
    
    [Id(1)] public LLMConfigDto LLMConfig { get; set; }
    
    [Id(3)]
    [DefaultValues(false, true)]
    public bool StreamingModeEnabled { get; set; }
    
    [Id(4)] public StreamingConfig StreamingConfig { get; set; }
    
    [Id(5)] public TimeSpan? RequestRecoveryDelay { get; set; }
    
    [Id(6)] public Guid ParentId { get; set; }
    
    [Id(7)]
    [DefaultValues(30, 10, 60, 120)]
    public int CheckInterval { get; set; } = 30;
    
    [Id(8)]
    [DefaultValues(300, 180, 600)]
    public int MaxStatusAge { get; set; } = 300;
    
    [Id(9)]
    [DefaultValues(true)]
    public bool EnableHealthCheck { get; set; } = true;
}