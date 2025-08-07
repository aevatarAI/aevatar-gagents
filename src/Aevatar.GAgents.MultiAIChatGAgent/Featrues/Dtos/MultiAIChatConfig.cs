using System.ComponentModel.DataAnnotations;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.MultiAIChatGAgent.Featrues.Dtos;

[GenerateSerializer]
public class MultiAIChatConfig : ConfigurationBase
{
    [Id(0)]
    [Required(ErrorMessage = "Instructions are required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Instructions must be between 10 and 2000 characters")]
    public string Instructions { get; set; } = "You are a helpful AI assistant with access to multiple language models";
    
    [Id(1)]
    [Range(1, 100, ErrorMessage = "Max History Count must be between 1 and 100")]
    public int MaxHistoryCount { get; set; } = 20;
    
    [Id(2)]
    public bool StreamingModeEnabled { get; set; } = true;
    
    [Id(3)] 
    public StreamingConfig StreamingConfig { get; set; }
    
    [Id(4)] 
    [Required(ErrorMessage = "At least one LLM configuration is required")]
    [MinLength(1, ErrorMessage = "At least one LLM configuration must be provided")]
    public List<LLMConfigDto> LLMConfigs { get; set; }
    
    [Id(5)] 
    public TimeSpan RequestRecoveryDelay { get; set; } = TimeSpan.FromMinutes(1);
    
    [Id(6)]
    [Required(ErrorMessage = "Primary Model is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Primary Model name must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\-\.\_]+$", ErrorMessage = "Primary Model name can only contain alphanumeric characters, hyphens, dots and underscores")]
    public string PrimaryModel { get; set; } = "gpt-4";
    
    [Id(7)]
    [Required(ErrorMessage = "Fallback Model is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Fallback Model name must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\-\.\_]+$", ErrorMessage = "Fallback Model name can only contain alphanumeric characters, hyphens, dots and underscores")]
    public string FallbackModel { get; set; } = "gpt-3.5-turbo";
    
    [Id(8)]
    [Range(1, 10, ErrorMessage = "Max Retries must be between 1 and 10")]
    public int MaxRetries { get; set; } = 3;
    
    [Id(9)]
    public bool EnableLoadBalancing { get; set; } = true;
}