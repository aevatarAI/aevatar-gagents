using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orleans;

namespace Aevatar.GAgents.AI.Options;

[GenerateSerializer]
public class LLMConfig : LLMProviderConfig
{
    [Id(0)] [Required] public string ModelName { get; set; } = string.Empty;

    [Id(1)] [Required] public string Endpoint { get; set; } = string.Empty;

    [Id(2)] [Required] public string ApiKey { get; set; } = string.Empty;

    [Id(3)] public Dictionary<string, object>? Memo { get; set; } = null;

    public bool Equal(LLMConfig other)
    {
        return ProviderEnum == other.ProviderEnum && ModelIdEnum == other.ModelIdEnum && ModelName == other.ModelName &&
               Endpoint == other.Endpoint && ApiKey == other.ApiKey;
    }
}