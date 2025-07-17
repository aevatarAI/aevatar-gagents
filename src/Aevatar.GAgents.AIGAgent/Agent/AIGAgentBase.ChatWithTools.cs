using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Agent;

/// <summary>
/// Partial class for AIGAgentBase that adds tool-aware chat capabilities
/// </summary>
public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    /// <summary>
    /// Chat with conversation history and automatic tool invocation
    /// </summary>
    /// <param name="prompt">The user prompt</param>
    /// <param name="imageKeys">Optional image keys</param>
    /// <param name="history">Conversation history</param>
    /// <param name="promptSettings">Execution settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="context">AI chat context</param>
    /// <returns>Response with tool call details</returns>
    public async Task<ChatWithDetailsResponse> ChatWithHistoryAndToolsAsync(
        string prompt,
        List<string>? imageKeys = null,
        List<ChatMessage>? history = null,
        ExecutionPromptSettings? promptSettings = null,
        CancellationToken cancellationToken = default,
        AIChatContextDto? context = null)
    {
        var response = new ChatWithDetailsResponse();
        var overallStartTime = DateTime.UtcNow;

        // Clear tool calls from previous request
        ClearToolCalls();

        try
        {
            Logger.LogInformation("Processing chat with history and tools: {Prompt}", prompt);

            // Get the kernel from brain
            var kernel = GetKernelFromBrain();
            if (kernel == null)
            {
                Logger.LogWarning("Kernel not available, falling back to base ChatWithHistory");
                var fallbackHistory = await ChatWithHistory(prompt, history, promptSettings,
                    cancellationToken, context, imageKeys);
                response.Response = fallbackHistory?.LastOrDefault()?.Content ?? "No response generated.";
                response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;
                return response;
            }

            // Get chat completion service from kernel
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Create chat history
            var chatHistory = new ChatHistory();

            // Add system message
            var systemMessage = State.PromptTemplate ??
                                "You are a helpful AI assistant. When asked to perform tasks, use the available tools to help provide accurate and complete responses.";
            chatHistory.AddSystemMessage(systemMessage);

            // Add existing conversation history if provided
            if (history != null)
            {
                foreach (var msg in history)
                {
                    switch (msg.ChatRole)
                    {
                        case ChatRole.User:
                            chatHistory.AddUserMessage(msg.Content);
                            break;
                        case ChatRole.Assistant:
                            chatHistory.AddAssistantMessage(msg.Content);
                            break;
                        case ChatRole.System:
                            chatHistory.AddSystemMessage(msg.Content);
                            break;
                    }
                }
            }

            // Add current user message
            chatHistory.AddUserMessage(prompt);

            Logger.LogInformation("Available tools in kernel: {Tools}",
                string.Join(", ", kernel.Plugins.SelectMany(p => p.Select(f => f.Name))));

            // Configure execution settings for automatic tool calling
            OpenAIPromptExecutionSettings executionSettings;
            if (promptSettings != null)
            {
                // Use provided settings but ensure tool behavior is set
                var temperature = double.TryParse(promptSettings.Temperature, out var temp) ? temp : 0.1;
                executionSettings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = temperature,
                    MaxTokens = promptSettings.MaxToken,
                };
            }
            else
            {
                // Default settings with auto tool invocation
                executionSettings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.1,
                    MaxTokens = 2000
                };
            }

            // Get response with automatic tool invocation
            Logger.LogInformation("[{Timestamp}] Starting LLM call with auto tool invocation",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"));

            var chatResponse = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken);

            response.Response = chatResponse.Content ?? "I couldn't generate a response.";

            // Copy collected tool calls
            response.ToolCalls = new List<ToolCallDetail>(CurrentToolCalls);

            // Update tool call history in state
            await UpdateToolCallHistoryAsync(response.ToolCalls);

            // Track token usage if available
            if (chatResponse.Metadata?.TryGetValue("Usage", out var usageObj) == true)
            {
                if (usageObj is Dictionary<string, object> usage)
                {
                    var inputTokens = usage.TryGetValue("InputTokens", out var input) ? Convert.ToInt32(input) : 0;
                    var outputTokens = usage.TryGetValue("OutputTokens", out var output) ? Convert.ToInt32(output) : 0;
                    var totalTokens = usage.TryGetValue("TotalTokens", out var total)
                        ? Convert.ToInt32(total)
                        : inputTokens + outputTokens;

                    var tokenUsage = new TokenUsageStateLogEvent()
                    {
                        GrainId = this.GetPrimaryKey(),
                        InputToken = inputTokens,
                        OutputToken = outputTokens,
                        TotalUsageToken = totalTokens,
                        CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    RaiseEvent(tokenUsage);
                }
            }

            response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;

            Logger.LogInformation("[{Timestamp}] Chat completed with {ToolCount} tool calls in {Duration}ms",
                DateTime.UtcNow.ToString("HH:mm:ss.fff"),
                response.ToolCalls.Count,
                response.TotalDurationMs);

            return response;
        }
        catch (TaskCanceledException)
        {
            var timeoutDuration = DateTime.UtcNow - overallStartTime;
            Logger.LogError("Chat completion timed out after {Duration}ms", timeoutDuration.TotalMilliseconds);
            response.Response = $"Request timed out after {timeoutDuration.TotalSeconds:F1} seconds. Please try again.";
            response.TotalDurationMs = (long)timeoutDuration.TotalMilliseconds;
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during chat with history and tools");
            response.Response = $"Error: {ex.Message}";
            response.TotalDurationMs = (long)(DateTime.UtcNow - overallStartTime).TotalMilliseconds;
            return response;
        }
    }

    /// <summary>
    /// Update tool call history in state
    /// </summary>
    private async Task UpdateToolCallHistoryAsync(List<ToolCallDetail> toolCalls)
    {
        if (toolCalls.Count == 0)
            return;

        var toolCallHistoryEvent = new AddToolCallHistoryStateLogEvent
        {
            ToolCalls = toolCalls,
            Timestamp = DateTime.UtcNow
        };

        RaiseEvent(toolCallHistoryEvent);
        await ConfirmEvents();
    }

    /// <summary>
    /// Get tool call history from state
    /// </summary>
    public Task<List<ToolCallHistoryEntry>> GetToolCallHistoryAsync()
    {
        return Task.FromResult(State.ToolCallHistory ?? new List<ToolCallHistoryEntry>());
    }

    /// <summary>
    /// Clear tool call history
    /// </summary>
    public async Task ClearToolCallHistoryAsync()
    {
        RaiseEvent(new ClearToolCallHistoryStateLogEvent());
        await ConfirmEvents();
    }


    /// <summary>
    /// State log event for adding tool call history
    /// </summary>
    [GenerateSerializer]
    public class AddToolCallHistoryStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public List<ToolCallDetail> ToolCalls { get; set; } = new();
        [Id(1)] public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// State log event for clearing tool call history
    /// </summary>
    [GenerateSerializer]
    public class ClearToolCallHistoryStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
    }
}