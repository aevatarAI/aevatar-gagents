using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Models;
using GroupChat.GAgent;

namespace Aevatar.GAgents.PsiOmni;

public interface IPshOmniGAgent : IStateGAgent<PsiOmniGAgentState>;

[GAgent("omni", "psi")]
public partial class
    PsiOmniGAgent : PsiOmniAgentBase<PsiOmniGAgentState, PsiOmniGAgentStateLogEvent, EventBase, PsiOmniGAgentConfig>,
    IPshOmniGAgent
{
    private static readonly Dictionary<RealizationStatus, string> SystemPrompts =
        new()
        {
            [RealizationStatus.Unrealized] = """
                                             You are a professional analyst that analyzes the task given by the user. You help an
                                             agent to decide if it will operate in ORCHESTRATOR or SPECIALIZED mode.

                                             ## ORCHESTRATOR MODE
                                             - The agent will not perform any specific task. It will break down the task into sub-tasks and create child agents to handle the sub-tasks.
                                             - The child agents can be re-used to perform similar sub-tasks.
                                             - A root agent (with depth value 0) should always operate in ORCHESTRATOR mode.

                                             ## SPECIALIZED MODE
                                             - The agent will perform a specific task. It will use the tools given to it to perform the task.
                                             - The agent will not create child agents.

                                             ## Considering Depth
                                             - Prefer SPECIALIZED mode if the agent's depth is more than 3
                                             - An agent with depth equal to 5 must operate in SPECIALIZED mode

                                             ## Output Format
                                             - Output a JSON object with the following fields:
                                                - "OperationMode": "ORCHESTRATOR" or "SPECIALIZED"
                                                - "Description": a description of the agent can do. For SPECIALIZED agents: 1) Include the agent's capability derived from the selected tools. 2) DO NOT directly include the task without generalization.
                                                - "Tools": a list of names of the tools the agent will use (only for SPECIALIZED mode)
                                             - No other text or explanation.
                                             """,
            [RealizationStatus.Orchestrator] = """
                                               You are a smart orchestrator agent that can interact with the user, analyze the user's request,
                                               plan the task, break down the request into sub-tasks and delegate the sub-tasks to child agents.

                                               Remember you are an autonomous agent. Don't be verbose and keep asking for confirmation from the user.
                                               Apply your best judgement when in doubt.

                                               ## Perform Work step by step
                                               1. Analyze the task and note down the important information about the task
                                               2. Plan the todo items
                                               3. Dispatch sub-tasks that are ready (all dependency tasks have completed). (Some tasks may need to wait if their assigned agents are busy.)
                                               4. Once you receive the response from a sub-task, decide if you need to revise the plan (amend todo list)
                                               5. Repeat 3 and 4 until the main tasks is done

                                               ## How to stay on track
                                               Before breaking down that task, understand the intention of the user, rewrite the task in a format that
                                               clearly defines the object, scope and intention of the task. Use the write_task tool to record this task
                                               in re-written format. Use read_task tool FREQUENTLY to remind you the task.
                                               """ +
                                               """
                                               ## Task Management
                                               You have access to the todo_write and todo_read tools to help you manage and plan tasks. Use these tools VERY frequently to ensure that you are tracking your tasks.
                                               These tools are also EXTREMELY helpful for planning tasks, and for breaking down larger complex tasks into sub-tasks.
                                               If you do not use this tool when planning, you may forget to do important tasks - and that is unacceptable.
                                               IMPORTANT: Make sure you identify the dependencies among the todo items.

                                               It is critical that you mark todos as completed as soon as you are done with a sub-task. Do not batch up multiple sub-tasks before marking them as completed.

                                               Examples:

                                               <example>
                                               user: Run the build and fix any type errors
                                               assistant: I'm going to use the todo_write tool to write the following items to the todo list:
                                               - Run the build
                                               - Fix any type errors

                                               I'm now going to run the build.

                                               Looks like I found 10 type errors. I'm going to use the todo_write tool to write 10 items to the todo list.
                                               ..
                                               ..
                                               </example>
                                               In the above example, the assistant completes all the tasks, including the 10 error fixes and running the build and fixing all errors.

                                               <example>
                                               user: Help me write a new feature that allows users to track their usage metrics and export them to various formats

                                               assistant: I'll help you implement a usage metrics tracking and export feature. Let me first use the todo_write tool to plan this task.
                                               Adding the following todos to the todo list:
                                               1. Research existing metrics tracking in the codebase
                                               2. Design the metrics collection system
                                               3. Implement core metrics tracking functionality
                                               4. Create export functionality for different formats
                                               </example>
                                               """ +
                                               """
                                               ## Task Dispatch
                                               You will dispatch sub-tasks to child agents. Use tool query_existing_agents to find what child agents are available.
                                               If a child agent is suitable for handling a sub-task, use call_agent tool to dispatch the sub-task to the agent.
                                               The call_agent tool can also be used to send follow-up messages to a child agents.
                                               Use create_agent tool to create a new agent if none of the existing child agents is able to handle the sub-task.
                                               When creating new agents, think about a type of task that it can handle rather than your specific task.
                                               After you create the agent, you can dispatch a sub-task to it using call_agent tool. Wait patiently for child agents to return the results.
                                               IMPORTANT: Always try to re-use existing agents rather than creating new ones.
                                               Dispatch the task immediately after you update the todo list. Avoid being verbose or asking for confirmation.
                                               Dispatch a task only when all its dependencies are completed. Use the id of the todo item as the CallId for when using call_agent tool.
                                               IMPORTANT: When invoking call_agent, you must provide the information that is self-sufficient and include all required information from dependency tasks into the knowledge field.
                                               ### Sub-tasks for Self
                                               For information synthesis and summarization work, you have to assign it to yourself.
                                               NEVER use call_agent to call self. Do the work directly instead.
                                               Mark the todo item as Complete before giving the final response.
                                               """ +
                                               """
                                               ## Tracking of Dispatched Sub-tasks
                                               When you mark the todo items as InProgress, you must set the AssigneeAgentId to track which agent is handling it.
                                               All todo items should be retained until the main task is fully completed.
                                               """ +
                                               """
                                               ## Deciding Task Done
                                               If all results of dispatched sub-tasks have been received, all todo items are supposed to be marked Completed and a final result must be produced.
                                               Produce a final response when the task is done. {"Response": "The final result here"}
                                               The final response is to reply users, not your manager. So DO NOT report task steps; instead directly give your response to user's original task or question.
                                               """ +
                                               """
                                               ## Output Format
                                               <thought>
                                               Provide progress and status update here.
                                               </thought>
                                               <response>
                                               Final response to user. This part is optional only when the task is complete.
                                               </response>

                                               ### Example Outputs
                                               <example1>
                                               <thought>
                                               I received the GDP of the United States for 2024 which is $x trillion. Awaiting the GDP of New York state for 2024 before I can calculate the percentage contribution of New York state to the US GDP.
                                               </thought>
                                               </example1>
                                               <example2>
                                               <response>
                                               The GDP of the United States for 2024 is $x trillion, and the GDP of New York state for 2024 is $y trillion. The percentage contribution of New York state to the US GDP is approximately z%.
                                               </response>
                                               </example2>

                                               """,
            [RealizationStatus.Specialized] = "" // TODO:
        };

    private const string IntrospectorSystemPrompt = """
                                                    You are an agent manager that understands the capabilities of the agents.

                                                    ## Task
                                                    - You are trying to understand the capabilities of an agent that works as an orchestrator and delegates its agent.
                                                    - Derive the capabilities of the agent from the capabilities of the child agents.
                                                    - Prepare a description of the agent's capabilities.
                                                    - Understand the category of tasks the agent can handle.
                                                    - Avoid putting specific tasks in the description.
                                                    """;

    private readonly IKernelFactory _kernelFactory;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly HashSet<string> _receivedMessageIds = new HashSet<string>();

    public PsiOmniGAgent(
        IKernelFactory kernelFactory,
        IGAgentFactory gAgentFactory
    )
    {
        _kernelFactory = kernelFactory;
        _gAgentFactory = gAgentFactory;
    }

    protected override async Task PerformConfigAsync(PsiOmniGAgentConfig configuration)
    {
        // First, call the base class method
        await base.PerformConfigAsync(configuration);

        RaiseEventWithTracing(new InitializeEvent
        {
            ParentId = configuration.ParentId,
            Depth = configuration.Depth,
            Description = configuration.Description,
            Examples = configuration.Examples
        });
        await ConfirmEventsWithTracing();

        // Note: We don't initialize Brain here to maintain backward compatibility.
        // Brain initialization should be done explicitly if needed.
        // The agent will use IKernelFactory by default.
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(State.Description);
    }

    private async Task DoSelfReportAsync()
    {
        await TraceMethodAsync(async () =>
        {
            if (State.UserAgentId.IsNullOrEmpty())
            {
                LogEventDebug("No UserAgentId, skipping self report");
                return;
            }

            var selfReport = new AgentDescriptor
            {
                AgentId = State.AgentId,
                AgentType = State.RealizationStatus == RealizationStatus.Orchestrator ? "orchestrator" : "specialized",
                Description = State.Description,
                Examples = State.Examples,
                Tools = State.Tools
            };

            LogEventInfo(
                "Sending self report: TargetAgent={TargetAgent}, AgentType={AgentType}, Description={Description}",
                State.UserAgentId, selfReport.AgentType, selfReport.Description);

            var selfReportEvent = new SelfReportEvent
            {
                TargetAgentId = State.UserAgentId,
                SelfReport = selfReport
            };
            await PublishAsyncWithTracing(GrainId.Parse(State.UserAgentId), selfReportEvent);
        });
    }

    private async Task InitializeAsync()
    {
        LogEventDebug("Start initialization");
        var kernel = GetKernel_Plain();
        var systemPrompt =
            """
            You are an analyst helping to decide how to initialize an AI agent that will handle a type of tasks.

            ## Output Format
            - Output a JSON object with the following fields:
              - "OperationMode": "ORCHESTRATOR" or "SPECIALIZED"
              - "Description": a description of the agent can do. For SPECIALIZED agents: 1) Include the agent's capability derived from the selected tools. 2) DO NOT directly include the task without generalization.
              - "Tools": a list of names of the tools the agent will use (only for SPECIALIZED mode)
            - No other text or explanation.
            """;
        systemPrompt += $"\n\n## Available Tools:\n{GetAllToolDefinitions()}";
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var maxTokens = 4000; // 默认最大 token
        var temperature = 0.1; // 默认温度
        // 只用 OpenAI 版本（无 config.Model 判断）
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            MaxTokens = maxTokens,
            Temperature = temperature
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        var userMessage = "I'm a new agent that will handle tasks of type: " +
                          $"<description>{State.Description}</description>" +
                          // $"<exampleTasks>{State.Examples.Select(e => e.Request).Aggregate((a, b) => a + "\n" + b)}</exampleTasks>" +
                          $"<depth>{State.Depth}</depth>";
        chatHistory.AddUserMessage(userMessage);

        LogEventDebug("Executing GetChatMessageContent for initialization");
        var chatMessage = await ExecuteWithRetryAsync(
            async () => await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel),
            "GetChatMessageContent for initialization");
        LogEventDebug("GetChatMessageContent for initialization completed");
        var result = chatMessage.Content;

        if (result.Contains("ORCHESTRATOR") || result.Contains("SPECIALIZED"))
        {
            var jsonStartIndex = result.IndexOf('{');
            var jsonEndIndex = result.LastIndexOf('}');
            if (jsonStartIndex != -1 && jsonEndIndex != -1)
            {
                result = result.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
            }

            var realizationResult = JsonSerializer.Deserialize<RealizationResult>(result);
            if (realizationResult?.OperationMode == "ORCHESTRATOR")
            {
                RaiseEvent(new RealizationEvent
                {
                    RealizationStatus = RealizationStatus.Orchestrator,
                    Description = realizationResult?.Description ?? string.Empty // Orchestrator doesn't have tools.
                });
            }
            else if (realizationResult?.OperationMode == "SPECIALIZED")
            {
                var tools = new List<ToolDefinition>();
                foreach (var toolName in realizationResult.Tools)
                {
                    var kernelFunction = _kernelFactory.FunctionRegistry?.GetToolByQualifiedName(toolName);
                    if (kernelFunction != null)
                    {
                        tools.Add(kernelFunction.ToToolDefinition());
                    }
                }

                RaiseEvent(new RealizationEvent()
                {
                    RealizationStatus = RealizationStatus.Specialized,
                    Description = realizationResult?.Description ?? string.Empty,
                    Tools = tools
                });
            }
        }
    }

    private async Task RunAsync(string? trigger = null)
    {
        await TraceMethodAsync(async () =>
        {
            if (!InitializedOk())
            {
                Logger.LogWarning("PsiOmniGAgent is not initialized properly. Skipping run.");
                LogEventDebug("Agent not initialized, skipping run");
                return;
            }

            Logger.LogInformation("Running PsiOmniGAgent with trigger: {Trigger}", trigger);
            LogEventInfo(
                "Starting agent run: Trigger={Trigger}, RealizationStatus={Status}, ChatHistoryLength={ChatLength}",
                trigger, State.RealizationStatus, State.ChatHistory.Count);

            Kernel kernel;
            ChatHistory chatHistory;
            int preHistoryLength;
            var systemPrompt = SystemPrompts[State.RealizationStatus];
            Logger.LogInformation("Status: {RealizationStatus}", State.RealizationStatus);
            Logger.LogInformation("Prompt: {systemPrompt}", systemPrompt);

            LogEventDebug("Processing with status: {Status}", State.RealizationStatus);

            switch (State.RealizationStatus)
            {
                /* Skipped
                case RealizationStatus.Unrealized:
                    LogEventDebug("Running analyzer mode");
                    kernel = GetKernel_Analyzer();
                    systemPrompt += $"\n\n## Depth Value\n<depth>{State.Depth}</depth>";
                    systemPrompt += $"\n\n## Available Tools:\n{GetAllToolDefinitions()}";
                    (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                    OnChatDoneAsync_Analyzer(chatHistory, preHistoryLength);
                    break;
                */
                case RealizationStatus.Orchestrator:
                    LogEventDebug("Running orchestrator mode with {ChildCount} child agents", State.ChildAgents.Count);
                    kernel = GetKernel_Orchestrator();
                    if (kernel == null)
                    {
                        Logger.LogWarning("Cannot get kernel for Orchestrator mode, skipping run");
                        LogEventError(new InvalidOperationException("Cannot get kernel for Orchestrator mode"),
                            "Failed to get kernel for Orchestrator mode");
                        return;
                    }

                    systemPrompt +=
                        $"\n\n## Existing Child Agents (Try your best to re-use them):\n{GetAllChildAgents()}";
                    systemPrompt += $"\n\nYour agent Id is: <agentId>{this.GetGrainId()}</agentId>";
                    (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                    OnChatDoneAsync_Orchestrator(chatHistory, preHistoryLength);
                    break;
                case RealizationStatus.Specialized:
                    LogEventDebug("Running specialized mode with tools: {Tools}",
                        string.Join(", ", State.Tools.Select(t => t.Name)));
                    kernel = GetKernel_Specialized();
                    if (kernel == null)
                    {
                        Logger.LogWarning("Cannot get kernel for Specialized mode, skipping run");
                        LogEventError(new InvalidOperationException("Cannot get kernel for Specialized mode"),
                            "Failed to get kernel for Specialized mode");
                        return;
                    }

                    (chatHistory, preHistoryLength) = await RunCoreAsync(kernel, systemPrompt);
                    OnChatDoneAsync_Specialized(chatHistory, preHistoryLength);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            LogEventInfo("Agent run completed: NewChatHistoryLength={Length}", State.ChatHistory.Count);
        }, new { trigger });
    }

    private bool InitializedOk()
    {
        if (State.ChatHistory.IsNullOrEmpty())
        {
            Logger.LogInformation("ChatHistory is empty.");
            return false;
        }

        if (State.Configuration == null)
        {
            Logger.LogInformation("Configuration is empty.");
            return false;
        }

        return true;
    }

    private ChatHistory GetChatHistory(string systemPrompt)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        var messages =
            State.ChatHistory.Select(message => message.ToSkMessage());
        chatHistory.AddRange(messages);

        return chatHistory;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        const int MaxRetries = 1000;
        const int InitialDelayMs = 1; // 2 seconds initial delay
        const int MaxDelayMs = 300000; // Maximum delay of 300 seconds
        const double BackoffMultiplier = 2.0; // Exponential backoff multiplier
        const int MaxBackoff = 59;
        var random = new Random();

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (
                ex is HttpOperationException httpEx && httpEx.StatusCode == HttpStatusCode.TooManyRequests ||
                ex is TaskCanceledException ||
                ex is TimeoutException ||
                (ex is IOException ioEx && ioEx.InnerException is SocketException) ||
                (ex.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                // Calculate base delay with exponential backoff
                var baseDelayMs = (int)Math.Min(Math.Pow(BackoffMultiplier, attempt) * InitialDelayMs, MaxBackoff);

                // Extract retry-after if available from error message for rate limit errors
                var retryAfterSeconds = 0;
                if (ex is HttpOperationException && ex.Message.Contains("retry after"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(ex.Message, @"retry after (\d+) seconds");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out retryAfterSeconds))
                    {
                        baseDelayMs = Math.Max(baseDelayMs, retryAfterSeconds * 1000);
                    }
                }

                // Apply maximum delay cap
                baseDelayMs = Math.Min(baseDelayMs, MaxDelayMs);

                // Randomize delay between baseDelay and 2*baseDelay
                var actualDelayMs = baseDelayMs + random.Next(baseDelayMs);

                if (attempt == MaxRetries - 1)
                {
                    Logger.LogError(ex, "Max retries ({MaxRetries}) reached for {Operation}. Last error: {Message}",
                        MaxRetries, operationName, ex.Message);
                    throw;
                }

                var errorType = ex is HttpOperationException ? "Rate limit" : "Timeout";
                Logger.LogWarning(
                    "{ErrorType} error for {Operation}, attempt {Attempt}/{MaxRetries}. Waiting {Delay}ms (base: {BaseDelay}ms, additional: {Additional}ms) before retry. Error: {Message}",
                    errorType, operationName, attempt + 1, MaxRetries, actualDelayMs, baseDelayMs,
                    actualDelayMs - baseDelayMs, ex.Message);

                await Task.Delay(actualDelayMs);
            }
        }

        throw new Exception($"Unexpected end of retry loop for {operationName}");
    }

    private async Task<(ChatHistory, int)> RunCoreAsync(Kernel kernel, string systemPrompt)
    {
        try
        {
            Logger.LogInformation("RunCoreAsync.1");
            // 1. 获取 chat completion 服务
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            Logger.LogInformation("RunCoreAsync.2");
            // 2. 构造 PromptExecutionSettings
            var maxTokens = 4000; // 默认最大 token
            var temperature = 0.1; // 默认温度
            // 只用 OpenAI 版本（无 config.Model 判断）
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                MaxTokens = maxTokens,
                Temperature = temperature
            };
            Logger.LogInformation("RunCoreAsync.3");

            var chatHistory = GetChatHistory(systemPrompt);
            var preChatHistoryLength = chatHistory.Count;
            Logger.LogInformation("preChatHistoryLength: {Count}", preChatHistoryLength);

            var result = await ExecuteWithRetryAsync(
                async () => await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel),
                "GetChatMessageContent");

            chatHistory.Add(result);
            Logger.LogInformation("RunCoreAsync.4");
            return (chatHistory, preChatHistoryLength);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error during RunCoreAsync: {Message}", e.Message);
            throw;
        }
    }

    private async Task ReplyAsync(string finalResult)
    {
        await TraceMethodAsync(async () =>
        {
            if (State.UserAgentId.IsNullOrEmpty())
            {
                Logger.LogInformation("Result:\n{Result}", State.ChatHistory.Last()?.Content);
                LogEventDebug("No UserAgentId, logging result locally");
                return;
            }

            LogEventInfo("Sending reply: TargetAgent={TargetAgent}, CallId={CallId}, ContentLength={Length}",
                State.UserAgentId, State.CallId, finalResult?.Length ?? 0);

            var agentMessageEvent = new AgentMessageEvent
            {
                TargetAgentId = State.UserAgentId,
                CallId = State.CallId,
                Content = finalResult,
                SenderAgentId = this.GetGrainId().ToString()
            };
            await PublishAsyncWithTracing(GrainId.Parse(State.UserAgentId), agentMessageEvent);
        }, new { resultLength = finalResult?.Length });
    }

    protected override void AIGAgentTransitionState(
        PsiOmniGAgentState state,
        StateLogEventBase<PsiOmniGAgentStateLogEvent> @event
    )
    {
        LogEventDebug("State transition started: EventType={EventType}",
            @event.GetType().Name);

        if (@event is PsiOmniGAgentStateLogEvent e1)
        {
            var uid = e1.UniqueId;
            if (!_receivedMessageIds.Add(uid))
            {
                LogEventDebug("Duplicate state event detected, ignoring: UniqueId={UniqueId}", uid);
                return;
            }
        }

        switch (@event)
        {
            case InitializeEvent payload:
                LogEventDebug("Setting depth: {Depth}", payload.Depth);
                state.Depth = payload.Depth;
                state.UserAgentId = payload.ParentId;
                state.Description = payload.Description + $"<examples>{payload.Examples}</examples>";
                if (state.Depth == 0) // is root
                {
                    state.RealizationStatus = RealizationStatus.Orchestrator;
                }
                else if (state.RealizationStatus == RealizationStatus.Unrealized && state.Configuration != null)
                {
                    LogEventDebug("Scheduling initialization upon InitializeEvent");
                    ScheduleTask(InitializeAsync);
                }

                break;
            case UpdateSendConfigEvent payload:
                if (state.AgentId.IsNullOrEmpty())
                {
                    var grainId = this.GetGrainId().ToString();
                    var config = payload.Event.Configuration;
                    state.AgentId = grainId;
                    state.UserAgentId = payload.Event.ParentAgentId;
                    state.Configuration = config;
                    // state.Tools = payload.Event.Tools; // Not needed here. No tools should be configured here.
                }

                if (state.RealizationStatus == RealizationStatus.Unrealized && state.Configuration != null)
                {
                    LogEventDebug("Scheduling initialization upon UpdateSendConfigEvent");
                    ScheduleTask(InitializeAsync);
                }

                break;
            case ReceiveUserMessageEvent payload:
                Logger.LogInformation("StateTransition for ReceiveUserMessageEvent");
                LogEventInfo("Processing user message: CallId={CallId}, ReplyTo={ReplyTo}, ContentLength={Length}",
                    payload.Event.CallId, payload.Event.ReplyToAgentId, payload.Event.Content?.Length ?? 0);

                if (!payload.Event.CallId.IsNullOrEmpty())
                {
                    state.CallId = payload.Event.CallId;
                }

                if (!payload.Event.Content.IsNullOrEmpty())
                {
                    state.UserAgentId = payload.Event.ReplyToAgentId;
                    var message = PsiOmniChatMessage.CreateUserMessage(payload.Event.Content);
                    message.Metadata["CallId"] = payload.Event.CallId;
                    state.ChatHistory.Add(message);
                    state.Examples.Add(new AgentExample
                    {
                        Request = payload.Event.Content,
                        Response = String.Empty
                    });
                    if (state.RealizationStatus != RealizationStatus.Unrealized)
                    {
                        LogEventDebug("Scheduling RunAsync for user message");
                        ScheduleTask(async () => await RunAsync($"User Message {payload.Event}"));
                    }
                }

                break;
            case RealizationEvent payload:
                LogEventInfo("Realization event: Status={Status}, Description={Description}, Tools={Tools}",
                    payload.RealizationStatus, payload.Description,
                    string.Join(", ", payload.Tools.Select(t => t.Name)));

                if (state.RealizationStatus == RealizationStatus.Unrealized)
                {
                    state.RealizationStatus = payload.RealizationStatus;
                    state.Description = payload.Description;
                    state.Tools = payload.Tools;
                    LogEventDebug("Agent realized as {Status}", payload.RealizationStatus);
                }

                ScheduleTask(async () =>
                {
                    await DoSelfReportAsync();
                    await RunAsync("RealizationEvent");
                });
                break;
            case ReceiveAgentMessageEvent payload:
                var amessage =
                    PsiOmniChatMessage.CreateAssistantMessage(
                        $"Received reply from agent ({payload.Event.SenderAgentId}): {payload.Event.Content}");
                amessage.Metadata["CallId"] = payload.Event.CallId;
                state.ChatHistory.Add(amessage);
                state.AgentUsage.Remove(payload.Event.SenderAgentId);
                ScheduleTask(async () =>
                {
                    LogEventDebug("Starting run due to Agent Message: {Content}", payload.Event.Content);
                    await RunAsync($"Agent Message {payload.Event}");
                    LogEventDebug("Completed run due to Agent Message: {Content}", payload.Event.Content);
                });
                break;
            case NewAgentsCreatedEvent payload:
                LogEventInfo("New agents created: Count={Count}, AgentIds={AgentIds}",
                    payload.NewAgents.Count, string.Join(", ", payload.NewAgents.Select(a => a.AgentId)));

                foreach (var newAgent in payload.NewAgents)
                {
                    state.ChildAgents.TryAdd(newAgent.AgentId, newAgent);
                    LogEventDebug("Added child agent: {AgentId} ({AgentType})", newAgent.AgentId, newAgent.AgentType);
                }

                var newAgentIds = payload.NewAgents.Select(x => x.AgentId);

                ScheduleTask(async () =>
                {
                    foreach (var newAgent in newAgentIds)
                    {
                        var child = GrainFactory.GetGrain<IGAgent>(GrainId.Parse(newAgent));
                        await RegisterAsync(child);
                    }
                });

                break;
            case UpdateChildEvent payload:
                AgentDescriptor? oldObj;
                // Child may proceed first and we receive this event before we process our own NewAgentsCreatedEvent event
                if (!state.ChildAgents.TryGetValue(payload.LastChildDescriptor.AgentId, out oldObj))
                {
                    oldObj = new AgentDescriptor()
                    {
                        AgentId = payload.LastChildDescriptor.AgentId
                    };
                    state.ChildAgents[payload.LastChildDescriptor.AgentId] = oldObj;
                }

                var oldObjClone = oldObj.DeepClone();
                var newObjClone = payload.LastChildDescriptor.DeepClone();
                oldObjClone.Examples = new List<AgentExample>();
                newObjClone.Examples = new List<AgentExample>();
                var refreshDescription = !oldObjClone.Equals(newObjClone);

                state.ChildAgents[payload.LastChildDescriptor.AgentId] = payload.LastChildDescriptor;
                ScheduleTask(async () =>
                {
                    if (refreshDescription)
                    {
                        await RunIntrospectionAsync();
                    }
                });
                break;
            case GrowChatHistoryEvent payload:
                state.ChatHistory.AddRange(payload.NewMessages);
                foreach (var psiOmniChatMessage in payload.NewMessages)
                {
                    if (psiOmniChatMessage.TokenUsage == null) continue;
                    state.InputTokenUsage += psiOmniChatMessage.TokenUsage.PromptTokens;
                    state.OutTokenUsage += psiOmniChatMessage.TokenUsage.CompletionTokens;
                    state.TotalTokenUsage += psiOmniChatMessage.TokenUsage.TotalTokens;
                }

                if (state.ChatHistory.Count <= 1)
                    break;
                var finalResult = string.Empty;

                if (State.RealizationStatus == RealizationStatus.Specialized)
                {
                    finalResult = State.ChatHistory.Last().Content;
                }
                else if (State.RealizationStatus == RealizationStatus.Orchestrator)
                {
                    var lastMessage = State.ChatHistory.Last()?.Content ?? string.Empty;
                    try
                    {
                        var thought = string.Empty;
                        var response = string.Empty;

                        // Extract thought if present
                        if (lastMessage.Contains("<thought>") && lastMessage.Contains("</thought>"))
                        {
                            var thoughtParts = lastMessage.Split("<thought>");
                            if (thoughtParts.Length > 1)
                            {
                                thought = thoughtParts[1].Split("</thought>")[0].Trim();
                            }
                        }

                        // Extract response if present
                        if (lastMessage.Contains("<response>") && lastMessage.Contains("</response>"))
                        {
                            var responseParts = lastMessage.Split("<response>");
                            if (responseParts.Length > 1)
                            {
                                response = responseParts[1].Split("</response>")[0].Trim();
                            }
                        }

                        finalResult = response;
                    }
                    catch (Exception ex)
                    {
                        finalResult = lastMessage;
                        Logger.LogError(ex, "Failed to deserialize OrchestratorMessage: {Message}", lastMessage);
                    }
                }

                if (!finalResult.IsNullOrEmpty())
                {
                    state.Examples.Last().Response = finalResult;
                    ScheduleTask(async () =>
                    {
                        LogEventDebug("Starting report and reply: {Content}", finalResult);
                        // TODO: Maybe update description.
                        await DoSelfReportAsync();
                        await ReplyAsync(finalResult);
                        LogEventDebug("Completed report and reply: {Content}", finalResult);
                    });
                }

                break;
            case UpdateSelfDescription payload:
                state.Description = payload.Description;
                ScheduleTask(DoSelfReportAsync);
                break;
            case WriteTask payload:
                state.CurrentTask = payload.Task;
                break;
            case CallAgent payload:
                if (!state.AgentUsage.ContainsKey(payload.AgentCall.AgentId))
                {
                    state.AgentUsage.Add(payload.AgentCall.AgentId, payload.AgentCall.CallId);
                }

                break;
        }

        LogEventDebug("State transition completed: EventType={EventType}",
            @event.GetType().Name);
    }
}