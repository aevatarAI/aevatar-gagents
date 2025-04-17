using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.GEvents;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Orleans;
using Orleans.SyncWork;

namespace Aevatar.AI.Feature.StreamSyncWoker;

public class BaseLongStreamWorker:StreamAsyncWorker<StreamRequest,AIStreamingResponseGEvent>
{
    public BaseLongStreamWorker(ILogger<StreamAsyncWorker<StreamRequest, AIStreamingResponseGEvent>> logger, LimitedConcurrencyLevelTaskScheduler limitedConcurrencyScheduler) : base(logger, limitedConcurrencyScheduler)
    {
    }

    protected override async Task<AIStreamingResponseGEvent> PerformLongRunTask(IStreamHandler<AIStreamingResponseGEvent> streamHandler, StreamRequest request)
    {
        var streamingConfig = request.StreamingConfig;
        var result = new InvokePromptResponse();
        var cancellationToken = new CancellationToken();
        if (streamingConfig?.TimeOutInternal > 0)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(streamingConfig.TimeOutInternal));
            cancellationToken = cts.Token;
        }
        
        // todo:get brain;
        
        var responseStreaming = await _brain.InvokePromptStreamingAsync(request.Content, request.History, request.IfUseKnowledge,
            request.PromptSettings,
            cancellationToken: cancellationToken);

        var chatList = new List<ChatMessage>();
        var chatMessage = new ChatMessage();
        var streamingMessageContentList = new List<object>();
        var bufferingSize = streamingConfig?.BufferingSize ?? 0;
        var stringBuilder = new StringBuilder();
        var completeContent = new StringBuilder();
        var chunkNumber = 0;

        await foreach (var messageContent in responseStreaming)
        {
            if (messageContent is StreamingChatMessageContent streamingChatMessageContent)
            {
                streamingMessageContentList.Add(streamingChatMessageContent);
                stringBuilder.Append(streamingChatMessageContent.Content);
                if (stringBuilder.Length >= bufferingSize)
                {
                    var chunk = stringBuilder.ToString(0, bufferingSize);
                    var aiStreamResponse = new AIStreamingResponseGEvent
                    {
                        Context = request.Context,
                        SerialNumber = chunkNumber++,
                        ResponseContent = chunk
                    };

                    await streamHandler.HandleStreamAsync(aiStreamResponse);
                    
                    completeContent.Append(chunk);
                    stringBuilder.Remove(0, bufferingSize);
                }

                if (streamingChatMessageContent.Role.HasValue)
                {
                    chatMessage.ChatRole = ConvertToChatRole(streamingChatMessageContent.Role.Value);
                }
            }
        }
        
        await streamHandler.HandleStreamAsync(new AIStreamingResponseGEvent
        {
            Context = request.Context,
            SerialNumber = chunkNumber,
            ResponseContent = stringBuilder.ToString(),
            IsLastChunk = true
        });
        completeContent.Append(stringBuilder.ToString());

        chatMessage.Content = completeContent.ToString();
        chatList.Add(chatMessage);
        result.TokenUsageStatistics = _brain.GetStreamingTokenUsage(streamingMessageContentList);
        result.ChatReponseList = chatList;

        return result;
    }
}

[GenerateSerializer]
public class StreamRequest
{
    public StreamingConfig StreamingConfig { get; set; }
    [Id(0)] public string Content { get; set; }
    [Id(1)] public List<ChatMessage>? History { get; set; }
    [Id(2)] public bool IfUseKnowledge { get; set; } = false;
    [Id(3)] public ExecutionPromptSettings? PromptSettings { get; set; } = null;
    [Id(4)] public AIChatContextDto Context { get; set; } = null;
}