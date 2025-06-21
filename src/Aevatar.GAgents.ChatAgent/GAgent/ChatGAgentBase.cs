using Aevatar.AI.Exceptions;
using Aevatar.AI.Feature.StreamSyncWoker;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.ChatAgent.Dtos;
using Aevatar.GAgents.ChatAgent.GAgent.State;
using Microsoft.Extensions.AI;
using ChatMessage = Aevatar.GAgents.AI.Common.ChatMessage;
using ChatRole = Aevatar.GAgents.AI.Common.ChatRole;

namespace Aevatar.GAgents.ChatAgent.GAgent;

public abstract class
    ChatGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IChatAgent
    where TState : ChatGAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ChatConfigDto
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Chat Agent");
    }

    public async Task<List<ChatMessage>?> ChatAsync(string message, ExecutionPromptSettings? promptSettings = null,
        AIChatContextDto? aiChatContextDto = null)
    {
        var result = await ChatWithHistory(message, State.ChatHistory, promptSettings, context: aiChatContextDto);

        if (result is not { Count: > 0 }) return result;

        var chatMessages = new List<ChatMessage>();
        chatMessages.Add(new ChatMessage() { ChatRole = ChatRole.User, Content = message });
        chatMessages.AddRange(result);

        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = chatMessages });

        await ConfirmEvents();

        return result;
    }

    /// <summary>
    /// 支持图片的聊天方法
    /// </summary>
    /// <param name="message">消息文本</param>
    /// <param name="imageBlobIds">图片Blob ID列表</param>
    /// <param name="promptSettings">提示设置</param>
    /// <param name="aiChatContextDto">聊天上下文</param>
    /// <returns>聊天响应消息列表</returns>
    public async Task<List<ChatMessage>?> ChatWithImagesAsync(string message, List<string>? imageBlobIds = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? aiChatContextDto = null)
    {
        var result = await ChatWithHistoryAndImages(message, imageBlobIds, State.ChatHistory, promptSettings, context: aiChatContextDto);

        if (result is not { Count: > 0 }) return result;

        var chatMessages = new List<ChatMessage>();
        var userMessage = new ChatMessage() 
        { 
            ChatRole = ChatRole.User, 
            Content = message,
            ImageBlobIds = imageBlobIds
        };
        chatMessages.Add(userMessage);
        chatMessages.AddRange(result);

        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = chatMessages });

        await ConfirmEvents();

        return result;
    }

    public async Task<bool> ChatWithStreamAsync(string message, AIChatContextDto context,
        ExecutionPromptSettings? promptSettings = null)
    {
        var result = await PromptWithStreamAsync(message, State.ChatHistory, promptSettings, context);
        if (!result) return result;

        var chatMessages = new List<ChatMessage>();
        chatMessages.Add(new ChatMessage() { ChatRole = ChatRole.User, Content = message });
        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = chatMessages });
        await ConfirmEvents();

        return result;
    }

    /// <summary>
    /// 支持图片的流式聊天方法
    /// </summary>
    /// <param name="message">消息文本</param>
    /// <param name="imageBlobIds">图片Blob ID列表</param>
    /// <param name="context">聊天上下文</param>
    /// <param name="promptSettings">提示设置</param>
    /// <returns>是否成功开始流式响应</returns>
    public async Task<bool> ChatWithImagesStreamAsync(string message, List<string>? imageBlobIds,
        AIChatContextDto context, ExecutionPromptSettings? promptSettings = null)
    {
        // 使用增强的提示进行流式处理
        var enhancedMessage = imageBlobIds != null && imageBlobIds.Any() 
            ? await BuildEnhancedPromptWithImagesForStream(message, imageBlobIds)
            : message;

        var result = await PromptWithStreamAsync(enhancedMessage, State.ChatHistory, promptSettings, context);
        if (!result) return result;

        var chatMessages = new List<ChatMessage>();
        var userMessage = new ChatMessage() 
        { 
            ChatRole = ChatRole.User, 
            Content = message,
            ImageBlobIds = imageBlobIds
        };
        chatMessages.Add(userMessage);
        RaiseEvent(new AddChatHistoryLogEvent() { ChatList = chatMessages });
        await ConfirmEvents();

        return result;
    }

    /// <summary>
    /// 为流式聊天构造增强提示的辅助方法
    /// </summary>
    private async Task<string> BuildEnhancedPromptWithImagesForStream(string originalMessage, List<string> imageBlobIds)
    {
        try
        {
            // 如果有Blob存储服务，使用AIGAgentBase的方法
            if (this is AIGAgentBase<ChatGAgentState, TStateLogEvent, TEvent, TConfiguration> aigAgent)
            {
                // 通过反射调用BuildEnhancedPromptWithImages（如果可访问）
                // 或者简化处理，只是提供图片ID信息
                var enhancedMessage = $"{originalMessage}\n\n包含图片: {string.Join(", ", imageBlobIds)}";
                return enhancedMessage;
            }
            return originalMessage;
        }
        catch
        {
            // 如果出错，返回原始消息
            return originalMessage;
        }
    }

    protected sealed override async Task AIChatHandleStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum,
        string? errorMessage,
        AIStreamChatContent? content)
    {
        if (content is { IsAggregationMsg: true })
        {
            RaiseEvent(new AddChatHistoryLogEvent()
            {
                ChatList = new List<ChatMessage>()
                    { new ChatMessage() { ChatRole = ChatRole.Assistant, Content = content.ResponseContent } }
            });

            await ConfirmEvents();
        }

        await HandleChatStreamAsync(context, errorEnum, errorMessage, content);
    }

    protected virtual Task HandleChatStreamAsync(AIChatContextDto context, AIExceptionEnum errorEnum, string? errorMessage,
        AIStreamChatContent? content)
    {
        return Task.CompletedTask;
    }

    protected sealed override async Task PerformConfigAsync(TConfiguration configuration)
    {
        await InitializeAsync(
            new InitializeDto()
            {
                Instructions = configuration.Instructions,
                LLMConfig = configuration.LLMConfig,
                StreamingModeEnabled = configuration.StreamingModeEnabled,
                StreamingConfig = configuration.StreamingConfig
            });
        var maxHistoryCount = configuration.MaxHistoryCount;
        if (maxHistoryCount > 100)
        {
            maxHistoryCount = 100;
        }

        if (maxHistoryCount == 0)
        {
            maxHistoryCount = 10;
        }

        RaiseEvent(new SetMaxHistoryCount() { MaxHistoryCount = maxHistoryCount });
        await ConfirmEvents();

        await ChatPerformConfigAsync(configuration);
    }

    protected virtual Task ChatPerformConfigAsync(TConfiguration configuration)
    {
        return Task.CompletedTask;
    }

    [GenerateSerializer]
    public class AddChatHistoryLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<ChatMessage> ChatList { get; set; }
    }

    [GenerateSerializer]
    public class SetMaxHistoryCount : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public int MaxHistoryCount { get; set; }
    }

    protected override void AIGAgentTransitionState(TState state,
        StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case AddChatHistoryLogEvent setChatHistoryLog:
                if (setChatHistoryLog.ChatList.Count > 0)
                {
                    state.ChatHistory.AddRange(setChatHistoryLog.ChatList);
                }

                if (state.ChatHistory.Count() > state.MaxHistoryCount)
                {
                    state.ChatHistory.RemoveRange(0, state.ChatHistory.Count() - state.MaxHistoryCount);
                }

                break;
            case SetMaxHistoryCount setMaxHistoryCount:
                state.MaxHistoryCount = setMaxHistoryCount.MaxHistoryCount;
                break;
        }
    }
}

public interface IChatAgent : IGAgent, IAIGAgent
{
    Task<List<ChatMessage>?> ChatAsync(string message,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? aiChatContextDto = null);

    Task<bool> ChatWithStreamAsync(string message, AIChatContextDto aiChatContextDto,
        ExecutionPromptSettings? promptSettings = null);

    /// <summary>
    /// 支持图片的聊天方法
    /// </summary>
    Task<List<ChatMessage>?> ChatWithImagesAsync(string message, List<string>? imageBlobIds = null,
        ExecutionPromptSettings? promptSettings = null, AIChatContextDto? aiChatContextDto = null);

    /// <summary>
    /// 支持图片的流式聊天方法
    /// </summary>
    Task<bool> ChatWithImagesStreamAsync(string message, List<string>? imageBlobIds,
        AIChatContextDto context, ExecutionPromptSettings? promptSettings = null);
}