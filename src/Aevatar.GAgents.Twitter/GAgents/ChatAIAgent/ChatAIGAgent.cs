using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Aevatar.GAgents.AIGAgent.Dtos;
using AIChatMessage = Aevatar.GAgents.AI.Common.ChatMessage;
using WorkflowChatMessage = GroupChat.GAgent.Feature.Common.ChatMessage;

namespace Aevatar.GAgents.Twitter.GAgents.ChatAIAgent;

[Description("AI chat agent with workflow support")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(ChatAIGAgent))]
public class ChatAIGAgent :
    GroupMemberGAgentBase<ChatAIGAgentState, ChatAIGAgentEvent, EventBase, ChatAIGAgentConfigDto>,
    IChatAIGAgent
{
    private readonly ILogger<ChatAIGAgent> _logger;

    public ChatAIGAgent(ILogger<ChatAIGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an AI chat agent capable of participating in workflows and handling conversations.");
    }

    // Implementation of GroupMemberGAgentBase abstract methods
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // AI chat agent always shows high interest in conversations
        return Task.FromResult(80);
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId,
        List<WorkflowChatMessage>? coordinatorMessages)
    {
        var response = new ChatResponse();

        if (coordinatorMessages == null || coordinatorMessages.Count == 0)
        {
                        // Let AI generate a default response based on its Instructions
            _logger.LogInformation($"{State.MemberName} generating default AI response based on Instructions");
            
            // Use Instructions as base context and let AI say something
            var promptWithInstructions = $"{State.PromptTemplate ?? ""} Please say something to start the conversation.";
            var defaultAiMessages = await ChatWithHistory(promptWithInstructions);
            var defaultResponse = defaultAiMessages?.FirstOrDefault()?.Content;

            // Save conversation to state
            RaiseEvent(new ChatResponseEvent()
            {
                Response = defaultResponse,
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();

            response.Content = defaultResponse;
            return response;
        }

        // Process the workflow messages
        var userMessage = string.Join(" ", coordinatorMessages.Select(m => m.Content));

        _logger.LogInformation($"{State.MemberName} processing workflow message: {userMessage}");

        // Use real AI through ChatWithHistory method
        var aiMessages = await ChatWithHistory(userMessage);
        var aiResponse = aiMessages?.FirstOrDefault()?.Content ??
                         $"{State.MemberName}: I'm having trouble processing your request.";

        // Save conversation to state
        RaiseEvent(new ChatResponseEvent()
        {
            Response = aiResponse,
            Timestamp = DateTime.UtcNow
        });
        await ConfirmEvents();

        response.Content = aiResponse;

        return response;
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        _logger.LogInformation($"{State.MemberName} workflow finished for blackboard {blackboardId}");
        return Task.CompletedTask;
    }

    // IChatAIGAgent interface implementation
    public Task<string> GetLastResponseAsync()
    {
        return Task.FromResult(State.LastResponse ?? "No response yet");
    }

    protected override async Task PerformConfigAsync(ChatAIGAgentConfigDto configuration)
    {
        // Call the base implementation to set MemberName
        await base.PerformConfigAsync(configuration);

        // Initialize the AI agent with the provided configuration
        await InitializeAsync(new InitializeDto()
        {
            Instructions = configuration.Instructions,
            LLMConfig = configuration.LLMConfig
        });

        _logger.LogDebug("PerformConfigAsync ChatAIGAgent configuration and initialization completed");
    }

    protected override void GroupMemberTransitionState(ChatAIGAgentState state,
        StateLogEventBase<ChatAIGAgentEvent> @event)
    {
        _logger.LogDebug("GroupMemberTransitionState: {data}, type:{type}",
            JsonConvert.SerializeObject(@event), @event.GetType().FullName);

        switch (@event)
        {
            case ChatResponseEvent chatResponseEvent:
                State.LastResponse = chatResponseEvent.Response;
                State.LastActivityTime = chatResponseEvent.Timestamp;
                State.TotalInteractions++;
                break;
        }
    }
}