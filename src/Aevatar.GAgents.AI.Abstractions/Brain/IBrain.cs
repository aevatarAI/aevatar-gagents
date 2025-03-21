using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Volo.Abp.DependencyInjection;

namespace Aevatar.GAgents.AI.Brain;

public interface IBrain : ITransientDependency
{
    LLMProviderEnum ProviderEnum { get; }
    ModelIdEnum ModelIdEnum { get; }

    Task InitializeAsync(LLMConfig llmConfig, string id, string description);

    Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null);

    Task<InvokePromptResponse?> InvokePromptAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default);

    Task<IAsyncEnumerable<object>> InvokePromptStreamingAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default);

    TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList);
}