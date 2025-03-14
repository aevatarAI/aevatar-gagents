using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Google.Protobuf.WellKnownTypes;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.State;

[GenerateSerializer]
public abstract class AIGAgentStateBase : StateBase
{
    // [Id(0)] public LLMConfig? LLM { get; set; }

    [Id(0)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(1)] public bool IfUpsertKnowledge { get; set; } = false;
    [Id(2)] public int InputTokenUsage { get; set; } = 0;
    [Id(3)] public int OutTokenUsage { get; set; } = 0;
    [Id(4)] public int TotalTokenUsage { get; set; } = 0;
    [Id(5)] public string SystemLLM { get; set; }
    [Id(6)] public LLMProviderEnum ProviderEnum { get; set; }
    [Id(7)] public ModelIdEnum ModelIdEnum { get; set; }
    [Id(8)] public string ModelName { get; set; } = string.Empty;
    [Id(9)] public string Endpoint { get; set; } = string.Empty;
    [Id(10)] public string ApiKey { get; set; } = string.Empty;
    [Id(11)] public Dictionary<string, object>? Memo { get; set; } = null;

    public bool LLMEqual(LLMConfig other)
    {
        return ProviderEnum == other.ProviderEnum && ModelIdEnum == other.ModelIdEnum && ModelName == other.ModelName &&
               Endpoint == other.Endpoint && ApiKey == other.ApiKey;
    }

    public bool HasLLM()
    {
        return !string.IsNullOrEmpty(ModelName) || !string.IsNullOrEmpty(Endpoint) || !string.IsNullOrEmpty(ApiKey);
    }

    public LLMConfigDto ConvertToLLMConfigDto()
    {
        return new LLMConfigDto()
        {
            SystemLLM = SystemLLM,
            SelfLLMConfig = new SelfLLMConfig()
            {
                ProviderEnum = ProviderEnum, ModelId = ModelIdEnum, ModelName = ModelName, Endpoint = Endpoint,
                ApiKey = ApiKey, Memo = Memo
            }
        };
    }
}