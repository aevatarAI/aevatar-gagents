using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;

[GenerateSerializer]
public class MultiAIChatConfig : ConfigurationBase
{
    [Id(0)]
    [DefaultValues("You are a helpful AI assistant with access to multiple language models")]
    public string Instructions { get; set; }
    
    [Id(1)]
    [DefaultValues(20, 10, 50, 100)]
    public int MaxHistoryCount { get; set; } = 20;
    
    [Id(2)]
    [DefaultValues(true, false)]
    public bool StreamingModeEnabled { get; set; }
    
    [Id(3)] public StreamingConfig StreamingConfig { get; set; }
    
    [Id(4)] public List<LLMConfigDto> LLMConfigs { get; set; }
    
    [Id(5)] public TimeSpan RequestRecoveryDelay { get; set; } = TimeSpan.FromMinutes(1);
    
    [Id(6)]
    [DefaultValues("gpt-4", "gpt-3.5-turbo", "claude-3-sonnet")]
    public string PrimaryModel { get; set; } = "gpt-4";
    
    [Id(7)]
    [DefaultValues("gpt-3.5-turbo", "gpt-4")]
    public string FallbackModel { get; set; } = "gpt-3.5-turbo";
    
    [Id(8)]
    [DefaultValues(3, 1, 5)]
    public int MaxRetries { get; set; } = 3;
    
    [Id(9)]
    [DefaultValues(true, false)]
    public bool EnableLoadBalancing { get; set; } = true;
}