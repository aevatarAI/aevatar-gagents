using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.Twitter.GAgents.DirectAIAgent;

[Description("Direct AI agent with independent functionality")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(DirectAIGAgent))]
public class DirectAIGAgent : AIGAgentBase<DirectAIGAgentState, DirectAIGAgentEvent, EventBase, DirectAIGAgentConfigDto>,
    IDirectAIGAgent
{
    private readonly ILogger<DirectAIGAgent> _logger;

    public DirectAIGAgent(ILogger<DirectAIGAgent> logger)
    {
        _logger = logger;
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Direct AI agent for independent chat and processing capabilities without workflow dependencies.");
    }

    public async Task<string?> ChatAsync(string message)
    {
        _logger.LogInformation("DirectAIGAgent processing message: {Message}", message);
        
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Please provide a message.";
            }
            
            // Use ChatWithHistory method from AIGAgentBase
            var aiMessages = await ChatWithHistory(message);
            return aiMessages?.FirstOrDefault()?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return "An error occurred while processing your request.";
        }
    }

    public Task<string> GetLastResponseAsync()
    {
        return Task.FromResult(State.LastResponse ?? "No response yet");
    }

    protected override async Task PerformConfigAsync(DirectAIGAgentConfigDto configuration)
    {
        _logger.LogInformation("Configuring DirectAIGAgent with Instructions: {Instructions}", configuration.Instructions);
        
        // Initialize the AI agent with the provided configuration
        await InitializeAsync(new InitializeDto()
        {
            Instructions = configuration.Instructions,
            LLMConfig = configuration.LLMConfig
        });
        
        _logger.LogInformation("DirectAIGAgent configuration and initialization completed");
    }
    
    protected override void AIGAgentTransitionState(DirectAIGAgentState state,
        StateLogEventBase<DirectAIGAgentEvent> @event)
    {
        _logger.LogDebug("DirectAIGAgent state transition: {EventType}", @event.GetType().Name);
        
        switch (@event)
        {
            case ChatResponseEvent chatResponseEvent:
                state.LastResponse = chatResponseEvent.Response;
                state.LastActivityTime = chatResponseEvent.Timestamp;
                state.TotalInteractions++;
                break;
        }
    }


} 