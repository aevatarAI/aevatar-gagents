using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.EmbeddedDataLoader;
using Aevatar.GAgents.SemanticKernel.ExtractContent;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Aevatar.GAgents.SemanticKernel.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using ChatMessage = Aevatar.GAgents.AI.Common.ChatMessage;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public abstract class BrainBase : IBrain
{
    public abstract LLMProviderEnum ProviderEnum { get; }
    public abstract ModelIdEnum ModelIdEnum { get; }

    protected Kernel? Kernel;
    protected string? PromptTemplate;

    protected readonly IKernelBuilderFactory KernelBuilderFactory;
    protected readonly ILogger Logger;
    protected readonly IOptions<RagConfig> RagConfig;
    protected string Description = string.Empty;

    protected BrainBase(IKernelBuilderFactory kernelBuilderFactory, ILogger logger, IOptions<RagConfig> ragConfig)
    {
        KernelBuilderFactory = kernelBuilderFactory;
        Logger = logger;
        RagConfig = ragConfig;
    }

    protected abstract Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder);

    protected abstract PromptExecutionSettings GetPromptExecutionSettings(ExecutionPromptSettings promptSettings);

    protected abstract TokenUsageStatistics GetTokenUsage(IReadOnlyCollection<ChatMessageContent> messageList);
    
    public async Task InitializeAsync(LLMConfig llmConfig, string id, string description)
    {
        Description = description;
        var kernelBuilder = KernelBuilderFactory.GetKernelBuilder(id);

        await ConfigureKernelBuilder(llmConfig, kernelBuilder);
        Kernel = kernelBuilder.Build();
    }

    public async Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files)
    {
        if (Kernel == null)
        {
            return false;
        }

        if (files == null || !files.Any())
        {
            return true;
        }

        var ragConfig = RagConfig.Value;
        foreach (var file in files)
        {
            var contentExtract = Kernel.Services.GetRequiredKeyedService<IExtractContent>(file.Type.ToString());
            var cancelToken = new CancellationToken();
            var extractContentList = await contentExtract.Extract(file, cancelToken);
            if (extractContentList.Count == 0)
            {
                continue;
            }

            foreach (var extractContent in extractContentList)
            {
                var dataLoader = Kernel.Services.GetRequiredService<IEmbeddedDataSaverProvider>();
                await dataLoader.StoreAsync(extractContent.Name, extractContent.Content,
                    ragConfig.DataLoadingBatchSize, ragConfig.MaxChunkCount,
                    ragConfig.DataLoadingBetweenBatchDelayInMilliseconds,
                    new CancellationToken());
            }
        }

        return true;
    }

    public async Task<InvokePromptResponse?> InvokePromptAsync(string content, List<ChatMessage>? history,
        bool ifUseKnowledge = false, ExecutionPromptSettings? promptSettings = null)
    {
        if (Kernel == null)
        {
            return null;
        }

        var result = new InvokePromptResponse();
        var requestContent = content;
        var chatHistory = GetChatHistory(history);
        if (ifUseKnowledge)
        {
            var supplementList = await LoadAsync(content);
            requestContent = SupplementPrompt(supplementList, content);
        }

        chatHistory.Add(new ChatMessageContent(AuthorRole.User, requestContent));

        var chatService = Kernel.GetRequiredService<IChatCompletionService>();

        PromptExecutionSettings? promptExecutionSettings = null;
        if (promptSettings != null)
        {
            promptExecutionSettings = GetPromptExecutionSettings(promptSettings);
        }

        var response = await chatService.GetChatMessageContentsAsync(chatHistory, promptExecutionSettings);

        var chatList = new List<ChatMessage>();
        chatList.AddRange(response.Select(item => new ChatMessage()
            { ChatRole = ConvertToChatRole(item.Role), Content = item.Content }));

        result.TokenUsageStatistics = GetTokenUsage(response);
        result.ChatReponseList = chatList;

        return result;
    }
    
    private ChatHistory GetChatHistory(List<ChatMessage>? historyList)
    {
        var result = new ChatHistory(Description);
        if (historyList == null || !historyList.Any())
        {
            return result;
        }

        foreach (var history in historyList)
        {
            result.Add(new ChatMessageContent(ConvertToAuthorRole(history.ChatRole), history.Content));
        }

        return result;
    }

    private AuthorRole ConvertToAuthorRole(ChatRole chatRole)
    {
        switch (chatRole)
        {
            case ChatRole.System:
                return AuthorRole.System;
            case ChatRole.Assistant:
                return AuthorRole.Assistant;
            default:
                return AuthorRole.User;
        }
    }

    private ChatRole ConvertToChatRole(AuthorRole authorRole)
    {
        if (authorRole == AuthorRole.System)
        {
            return ChatRole.System;
        }

        return authorRole == AuthorRole.Assistant ? ChatRole.Assistant : ChatRole.User;
    }

    private async Task<List<TextSnippet<Guid>>> LoadAsync(string input)
    {
        var result = new List<TextSnippet<Guid>>();
        if (Kernel == null)
        {
            return result;
        }

        var searchCollection = Kernel.Services.GetRequiredService<VectorStoreTextSearch<TextSnippet<Guid>>>();
        var searchResults = await searchCollection.GetTextSearchResultsAsync(input);
        if (searchResults.TotalCount == 0)
        {
            return result;
        }

        await foreach (var textSnippet in searchResults.Results)
        {
            result.Add(new TextSnippet<Guid>
            {
                Text = textSnippet.Value,
                ReferenceDescription = textSnippet.Name,
                ReferenceLink = textSnippet.Link,
                Key = default
            });
            if (result.Count >= 10)
            {
                return result;
            }
        }

        return result;
    }

    private string SupplementPrompt(List<TextSnippet<Guid>> textSnippetList, string content)
    {
        if (!textSnippetList.Any())
        {
            return content;
        }

        var supplementInfo = new StringBuilder();
        supplementInfo.AppendLine("The following is additional information about the user's question.");

        foreach (var item in textSnippetList)
        {
            supplementInfo.AppendLine($"## {item.Text}");
            if (string.IsNullOrWhiteSpace(item.ReferenceDescription) == false)
            {
                supplementInfo.AppendLine($"    Reference Description:{item.ReferenceDescription}");
            }

            if (string.IsNullOrWhiteSpace(item.ReferenceLink) == false)
            {
                supplementInfo.AppendLine($"    Reference Link:{item.ReferenceLink}");
            }
        }

        supplementInfo.AppendLine($"The user's question is:{content} ");

        return supplementInfo.ToString();
    }

}