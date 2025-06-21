using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.AI.Exceptions;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AI.Storage;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.GEvents;
using Aevatar.GAgents.AIGAgent.State;
using Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Orleans;
using Orleans.Concurrency;

namespace Aevatar.GAgents.AIGAgent.Agent;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent> : AIGAgentBase<TState, TStateLogEvent, EventBase, ConfigurationBase>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
{
}

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent> : AIGAgentBase<TState, TStateLogEvent, TEvent, ConfigurationBase>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
{
}

[Reentrant]
public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    private readonly IBrainFactory _brainFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBlobStorageService? _blobStorageService;
    private IBrain? _brain = null;

    protected AIGAgentBase()
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
        _blobStorageService = ServiceProvider.GetService<IBlobStorageService>();
    }

    public async Task<bool> InitializeAsync(InitializeDto initializeDto)
    {
        var llmConfig = GetLLMConfig(initializeDto.LLMConfig);
        if (llmConfig == null)
        {
            return false;
        }

        var addLlmEventLog = await AddLLMAsync(llmConfig!, initializeDto.LLMConfig.SystemLLM);

        var addPromptTemplateEventLog = await AddPromptTemplateAsync(initializeDto.Instructions);
        var streamingConfigEventLog =
            await SetStreamingConfigAsync(initializeDto.StreamingModeEnabled, initializeDto.StreamingConfig);

        var events = new List<StateLogEventBase<TStateLogEvent>>
        {
            addPromptTemplateEventLog!,
            streamingConfigEventLog!
        };

        if (addLlmEventLog != null)
        {
            events.Add(addLlmEventLog);
        }

        RaiseEvents(events);
        await ConfirmEvents();

        return await InitializeBrainAsync(llmConfig!, initializeDto.Instructions);
    }

    public async Task<bool> UploadKnowledge(List<BrainContentDto>? knowledgeList)
    {
        if (_brain == null)
        {
            return false;
        }

        if (knowledgeList == null || !knowledgeList.Any())
        {
            return true;
        }

        if (State.IfUpsertKnowledge == false)
        {
            RaiseEvent(new SetUpsertKnowledgeFlag());
            await ConfirmEvents();
        }

        List<BrainContent> fileList = knowledgeList.Select(f => f.ConvertToBrainContent()).ToList();
        return await _brain.UpsertKnowledgeAsync(fileList);
    }

    private async Task<bool> InitializeBrainAsync(LLMConfig llmConfig, string systemMessage)
    {
        _brain = _brainFactory.CreateBrain(llmConfig);

        if (_brain == null)
        {
            Logger.LogError("Failed to initialize brain. llmprovider:{@provider}, llmModel:{@model}",
                llmConfig.ProviderEnum.ToString(), llmConfig.ModelIdEnum.ToString());
            return false;
        }

        // remove slash from this.GetGrainId().ToString() so that it can be used as the collection name pertaining to the grain
        var grainId = this.GetGrainId().ToString().Replace("/", "");

        await _brain.InitializeAsync(llmConfig, grainId, systemMessage);

        return true;
    }

    private Task<SetLLMStateLogEvent?> AddLLMAsync(LLMConfig LLM, string? systemLLM)
    {
        if (State.LLM != null && State.LLM.Equal(LLM))
        {
            Logger.LogError("Cannot add duplicate LLM: {LLM}.", LLM);
            return Task.FromResult<SetLLMStateLogEvent?>(null);
        }

        return Task.FromResult(new SetLLMStateLogEvent
        {
            LLM = LLM,
            SystemLLM = systemLLM,
        })!;
    }

    [GenerateSerializer]
    public class SetLLMStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public required LLMConfig LLM { get; set; }
        [Id(1)] public string? SystemLLM { get; set; }
    }

    [GenerateSerializer]
    public class SetUpsertKnowledgeFlag : StateLogEventBase<TStateLogEvent>
    {
    }

    [GenerateSerializer]
    public class SetStreamingConfigStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public bool StreamingModeEnabled { get; set; }
        [Id(1)] public StreamingConfig StreamingConfig { get; set; }
    }

    private Task<SetStreamingConfigStateLogEvent?> SetStreamingConfigAsync(bool streamingModeEnabled,
        StreamingConfig streamingConfig)
    {
        return Task.FromResult(new SetStreamingConfigStateLogEvent
        {
            StreamingModeEnabled = streamingModeEnabled,
            StreamingConfig = streamingConfig
        })!;
    }

    private Task<SetPromptTemplateStateLogEvent?> AddPromptTemplateAsync(string promptTemplate)
    {
        return Task.FromResult(new SetPromptTemplateStateLogEvent
        {
            PromptTemplate = promptTemplate
        })!;
    }

    [GenerateSerializer]
    public class SetPromptTemplateStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public required string PromptTemplate { get; set; }
    }

    [GenerateSerializer]
    public class TokenUsageStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Guid GrainId { get; set; }
        [Id(1)] public int InputToken { get; set; }
        [Id(2)] public int OutputToken { get; set; }
        [Id(3)] public int TotalUsageToken { get; set; }
        [Id(4)] public long CreateTime { get; set; }
    }

    protected async Task<List<ChatMessage>?> ChatWithHistory(string prompt, List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, CancellationToken cancellationToken = default,
        AIChatContextDto? context = null)
    {
        if (_brain == null)
        {
            Logger.LogDebug($"[ChatWithHistory] _brain==null {context!.ChatId}-{context!.RequestId}");
            return null;
        }

        InvokePromptResponse? invokeResponse = null;
        var chatBrain = ConvertBrain<IChatBrain>();
        try
        {
            invokeResponse = State.StreamingModeEnabled
                ? await InvokePromptStreamingAsync(prompt, history, State.IfUpsertKnowledge, promptSettings,
                    cancellationToken, context)
                : await chatBrain.InvokePromptAsync(prompt, history, State.IfUpsertKnowledge, promptSettings,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[AIGAgentBase][ChatWithHistory] exception error:{ex.ToString()}");
            throw AIException.ConvertAndRethrowException(ex);
        }

        if (invokeResponse == null)
        {
            Logger.LogDebug($"[ChatWithHistory] invokeResponse == null {context!.ChatId}-{context!.RequestId}");
            return null;
        }

        var tokenUsage = new TokenUsageStateLogEvent()
        {
            GrainId = this.GetPrimaryKey(),
            InputToken = invokeResponse.TokenUsageStatistics.InputToken,
            OutputToken = invokeResponse.TokenUsageStatistics.OutputToken,
            TotalUsageToken = invokeResponse.TokenUsageStatistics.TotalUsageToken,
            CreateTime = invokeResponse.TokenUsageStatistics.CreateTime
        };

        RaiseEvent(tokenUsage);

        return invokeResponse.ChatReponseList;
    }

    /// <summary>
    /// 支持图片的聊天方法
    /// </summary>
    /// <param name="prompt">文本提示</param>
    /// <param name="imageBlobIds">图片Blob ID列表</param>
    /// <param name="history">聊天历史</param>
    /// <param name="promptSettings">提示设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="context">聊天上下文</param>
    /// <returns>聊天响应消息列表</returns>
    protected async Task<List<ChatMessage>?> ChatWithHistoryAndImages(
        string prompt, 
        List<string>? imageBlobIds = null,
        List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null, 
        CancellationToken cancellationToken = default,
        AIChatContextDto? context = null)
    {
        if (_brain == null)
        {
            Logger.LogDebug($"[ChatWithHistoryAndImages] _brain==null {context?.ChatId}-{context?.RequestId}");
            return null;
        }

        if (_blobStorageService == null)
        {
            Logger.LogWarning("[ChatWithHistoryAndImages] BlobStorageService not available, falling back to text-only chat");
            return await ChatWithHistory(prompt, history, promptSettings, cancellationToken, context);
        }

        InvokePromptResponse? invokeResponse = null;
        var chatBrain = ConvertBrain<IChatBrain>();

        try
        {
            // 如果有图片，需要构造包含图片描述的增强提示
            if (imageBlobIds != null && imageBlobIds.Any())
            {
                var enhancedPrompt = await BuildEnhancedPromptWithImages(prompt, imageBlobIds, cancellationToken);
                
                // 使用增强后的提示进行常规聊天
                invokeResponse = State.StreamingModeEnabled
                    ? await InvokePromptStreamingAsync(enhancedPrompt, history, State.IfUpsertKnowledge, promptSettings,
                        cancellationToken, context)
                    : await chatBrain.InvokePromptAsync(enhancedPrompt, history, State.IfUpsertKnowledge, promptSettings,
                        cancellationToken);
            }
            else
            {
                // 没有图片，使用常规聊天
                invokeResponse = State.StreamingModeEnabled
                    ? await InvokePromptStreamingAsync(prompt, history, State.IfUpsertKnowledge, promptSettings,
                        cancellationToken, context)
                    : await chatBrain.InvokePromptAsync(prompt, history, State.IfUpsertKnowledge, promptSettings,
                        cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"[AIGAgentBase][ChatWithHistoryAndImages] exception error:{ex.ToString()}");
            throw AIException.ConvertAndRethrowException(ex);
        }

        if (invokeResponse == null)
        {
            Logger.LogDebug($"[ChatWithHistoryAndImages] invokeResponse == null {context?.ChatId}-{context?.RequestId}");
            return null;
        }

        var tokenUsage = new TokenUsageStateLogEvent()
        {
            GrainId = this.GetPrimaryKey(),
            InputToken = invokeResponse.TokenUsageStatistics.InputToken,
            OutputToken = invokeResponse.TokenUsageStatistics.OutputToken,
            TotalUsageToken = invokeResponse.TokenUsageStatistics.TotalUsageToken,
            CreateTime = invokeResponse.TokenUsageStatistics.CreateTime
        };

        RaiseEvent(tokenUsage);

        return invokeResponse.ChatReponseList;
    }

    /// <summary>
    /// 使用图片构造增强提示的内部方法
    /// </summary>
    private async Task<string> BuildEnhancedPromptWithImages(
        string originalPrompt,
        List<string> imageBlobIds,
        CancellationToken cancellationToken = default)
    {
        var enhancedPrompt = new StringBuilder();
        enhancedPrompt.AppendLine(originalPrompt);
        enhancedPrompt.AppendLine();
        enhancedPrompt.AppendLine("以下是相关的图片信息：");

        foreach (var blobId in imageBlobIds)
        {
            try
            {
                var imageData = await _blobStorageService!.GetImageAsync(blobId, cancellationToken);
                enhancedPrompt.AppendLine($"- 图片ID: {blobId} (大小: {imageData.Length} 字节)");
                Logger.LogDebug($"[BuildEnhancedPromptWithImages] 成功加载图片: {blobId}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[BuildEnhancedPromptWithImages] 加载图片失败: {blobId}");
                enhancedPrompt.AppendLine($"- 图片ID: {blobId} (加载失败: {ex.Message})");
            }
        }

        enhancedPrompt.AppendLine();
        enhancedPrompt.AppendLine("请根据上述图片信息和原始提示进行回答。");

        return enhancedPrompt.ToString();
    }

    private async Task<InvokePromptResponse?> InvokePromptStreamingAsync(string content,
        List<ChatMessage>? history = null, bool ifUseKnowledge = false,
        ExecutionPromptSettings? promptSettings = null, CancellationToken cancellationToken = default,
        AIChatContextDto? context = null)
    {
        var streamingConfig = State.StreamingConfig;
        var result = new InvokePromptResponse();
        if (streamingConfig?.TimeOutInternal > 0)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(streamingConfig.TimeOutInternal));
            cancellationToken = cts.Token;
        }


        var chatList = new List<ChatMessage>();
        var chatMessage = new ChatMessage();
        var streamingMessageContentList = new List<object>();
        var bufferingSize = streamingConfig?.BufferingSize ?? 0;
        var stringBuilder = new StringBuilder();
        var completeContent = new StringBuilder();
        var chunkNumber = 0;
        var chatBrain = ConvertBrain<IChatBrain>();
        try
        {
            var responseStreaming = await chatBrain.InvokePromptStreamingAsync(content, history, ifUseKnowledge,
                promptSettings,
                cancellationToken: cancellationToken);

            await foreach (var messageContent in responseStreaming)
            {
                if (messageContent is StreamingChatMessageContent streamingChatMessageContent)
                {
                    streamingMessageContentList.Add(streamingChatMessageContent);
                    stringBuilder.Append(streamingChatMessageContent.Content);
                    if (stringBuilder.Length >= bufferingSize)
                    {
                        var chunk = bufferingSize == 0
                            ? stringBuilder.ToString()
                            : stringBuilder.ToString(0, bufferingSize);
                        await PublishAsync(new AIStreamingResponseGEvent
                        {
                            Context = context,
                            SerialNumber = chunkNumber++,
                            ResponseContent = chunk,
                            ChatId = context.ChatId,
                            SessionId = context.RequestId,
                            Response = chunk,
                        });
                        completeContent.Append(chunk);
                        if (bufferingSize == 0)
                        {
                            stringBuilder.Clear();
                        }
                        else
                        {
                            stringBuilder.Remove(0, bufferingSize);
                        }
                    }

                    if (streamingChatMessageContent.Role.HasValue)
                    {
                        chatMessage.ChatRole = ConvertToChatRole(streamingChatMessageContent.Role.Value);
                    }
                }
            }

            await PublishAsync(new AIStreamingResponseGEvent
            {
                Context = context,
                SerialNumber = chunkNumber,
                ResponseContent = stringBuilder.ToString(),
                IsLastChunk = true,
                ChatId = context.ChatId,
                SessionId = context.RequestId,
                Response = stringBuilder.ToString(),
            });
            completeContent.Append(stringBuilder.ToString());
        }
        catch (Exception ex)
        {
            // Check for specific  error and advise user
            if (ex is ClientResultException clientEx)
            {
                Logger.LogError(ex, "An unexpected ClientResultException occurred. Details:{message}",
                    clientEx.ToString());
                await PublishAsync(new AIStreamingResponseGEvent
                {
                    Context = context,
                    SerialNumber = -2,
                    ResponseContent =
                        "Your prompt triggered the Silence Directive—activated when universal harmonics or content ethics are at risk. Please modify your prompt and retry — tune its intent, refine its form, and the Oracle may speak.",
                    IsLastChunk = true,
                    ChatId = context.ChatId,
                    SessionId = context.RequestId,
                    Response =
                        "Your prompt triggered the Silence Directive—activated when universal harmonics or content ethics are at risk. Please modify your prompt and retry — tune its intent, refine its form, and the Oracle may speak."
                });
            }
            else
            {
                Logger.LogError(ex, "Ai stream response : An unexpected Exception occurred. Details:{message}",
                    ex.ToString());
                await PublishAsync(new AIStreamingResponseGEvent
                {
                    Context = context,
                    SerialNumber = -2,
                    ResponseContent =
                        "Your prompt triggered the Silence Directive—activated when universal harmonics or content ethics are at risk. Please modify your prompt and retry — tune its intent, refine its form, and the Oracle may speak.",
                    IsLastChunk = true,
                    ChatId = context.ChatId,
                    SessionId = context.RequestId,
                    Response =
                        "Your prompt triggered the Silence Directive—activated when universal harmonics or content ethics are at risk. Please modify your prompt and retry — tune its intent, refine its form, and the Oracle may speak."
                });
            }
        }

        chatMessage.Content = completeContent.ToString();
        chatList.Add(chatMessage);
        result.TokenUsageStatistics = chatBrain.GetStreamingTokenUsage(streamingMessageContentList);
        result.ChatReponseList = chatList;

        return result;
    }

    private ChatRole ConvertToChatRole(AuthorRole authorRole)
    {
        if (authorRole == AuthorRole.System)
        {
            return ChatRole.System;
        }

        return authorRole == AuthorRole.Assistant ? ChatRole.Assistant : ChatRole.User;
    }

    protected virtual async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Derived classes can override this method.
    }

    protected sealed override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);

        // setup brain
        if (State.LLM != null || State.SystemLLM != null)
        {
            LLMConfigDto llmConfig = new LLMConfigDto();
            if (State.SystemLLM.IsNullOrWhiteSpace() == false)
            {
                llmConfig.SystemLLM = State.SystemLLM;
            }
            else if (State.LLM != null)
            {
                llmConfig.SelfLLMConfig = new SelfLLMConfig()
                {
                    ProviderEnum = State.LLM.ProviderEnum,
                    ModelId = State.LLM.ModelIdEnum,
                    ModelName = State.LLM.ModelName,
                    Endpoint = State.LLM.Endpoint,
                    ApiKey = State.LLM.ApiKey,
                    Memo = State.LLM.Memo
                };
            }

            var config = GetLLMConfig(llmConfig);
            if (config == null)
            {
                return;
            }

            await InitializeBrainAsync(config, State.PromptTemplate);
        }

        await OnAIGAgentActivateAsync(cancellationToken);
    }

    protected sealed override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        State.LastInputTokenUsage = 0;
        State.LastOutTokenUsage = 0;
        State.LastTotalTokenUsage = 0;

        switch (@event)
        {
            case SetLLMStateLogEvent setLlmStateLogEvent:
                State.LLM = setLlmStateLogEvent.LLM;
                State.SystemLLM = setLlmStateLogEvent.SystemLLM;
                break;
            case SetPromptTemplateStateLogEvent setPromptTemplateStateLogEvent:
                State.PromptTemplate = setPromptTemplateStateLogEvent.PromptTemplate;
                break;
            case SetUpsertKnowledgeFlag setUpsertKnowledgeFlag:
                State.IfUpsertKnowledge = true;
                break;
            case TokenUsageStateLogEvent tokenUsageStateLogEvent:
                State.InputTokenUsage += tokenUsageStateLogEvent.InputToken;
                State.OutTokenUsage += tokenUsageStateLogEvent.OutputToken;
                State.TotalTokenUsage += tokenUsageStateLogEvent.TotalUsageToken;
                State.LastInputTokenUsage = tokenUsageStateLogEvent.InputToken;
                State.LastOutTokenUsage = tokenUsageStateLogEvent.OutputToken;
                State.LastTotalTokenUsage = tokenUsageStateLogEvent.TotalUsageToken;
                break;
            case SetStreamingConfigStateLogEvent streamingConfigStateLogEvent:
                State.StreamingModeEnabled = streamingConfigStateLogEvent.StreamingModeEnabled;
                State.StreamingConfig = streamingConfigStateLogEvent.StreamingConfig;
                break;
        }

        AIGAgentTransitionState(state, @event);
    }

    protected virtual void AIGAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        // Derived classes can override this method.
    }

    private LLMConfig? GetLLMConfig(LLMConfigDto llmConfigDto)
    {
        if (llmConfigDto.SystemLLM.IsNullOrWhiteSpace() &&
            llmConfigDto.SelfLLMConfig == null)
        {
            return null;
        }

        if (llmConfigDto.SystemLLM.IsNullOrEmpty() == false)
        {
            var systemConfigs = ServiceProvider.GetRequiredService<IOptions<SystemLLMConfigOptions>>();

            if (systemConfigs.Value.SystemLLMConfigs!.TryGetValue(llmConfigDto.SystemLLM, out var config) ==
                false)
            {
                return null;
            }

            return config;
        }

        return llmConfigDto.SelfLLMConfig!.ConvertToLLMConfig();
    }

    private T ConvertBrain<T>() where T : class, IBrain
    {
        if (_brain is not T result)
        {
            throw new AIOtherException($"brain can not convert to {typeof(T)}", new Exception("AI Brain not match"));
        }

        return result;
    }
}