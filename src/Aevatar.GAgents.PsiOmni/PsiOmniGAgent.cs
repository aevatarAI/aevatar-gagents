using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Models;
using Aevatar.GAgents.AI.Common;
using GroupChat.GAgent;
using JsonConverter = Newtonsoft.Json.JsonConvert;

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
                                               DO NOT dispatch multiple sub-tasks to the same agent. Instead, wait until the agent to reply with the result before dispatching the next sub-task.
                                               If you are not sure whether the agents are busy, use the query_existing_agents tool to find the information.
                                               If you falsely dispatch multiple sub-tasks to the same agent, the call_agent tool will return an error. In this case, you can dispatch the sub-task again after the agent has replied.

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
                                               Produce a final response when the task is done.
                                               If an artifact needs to be returned, please include it in the result.
                                               The final response is to reply users, not your manager. So DO NOT report task steps; instead directly give your response to user's original task or question.
                                               """ +
                                               """
                                               ## Output Format
                                               Your output must contain the following three tags.
                                               1. When the task is not completed (pending more todo items), add progress in a <thought> tag.
                                                  If you are handling a sub-task by yourself, use write_artifact tool to output the step wise result.
                                                  Alternatively, for short result, you can directly output the step wise result using a <step_wise_result> tag.
                                               2. When the task is completed, provide your final response to user in a <repsonse> tag
                                               3. Optionally, if artifacts need to be returned to user. Include one or more <artifact> tag

                                               You MUST follow this format. An output without any of the tags is not valid.
                                               <thought>
                                               Provide progress and status update here.
                                               </thought>
                                               <step_wise_result>
                                               Step wise result here.
                                               </step_wise_result>
                                               <response>
                                               Final response to user. This part is optional only when the task is complete.
                                               </response>
                                               <artifact name="artifact_name.md" format="markdown" />

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
                                               <example3>
                                               <response>
                                               I have completed the research report for AI techniques and please find the report in the research_report.md file.
                                               </response>
                                               <artifact name="research_report.md" format="markdown" />
                                               </example3>
                                               <example4>
                                               <thought>
                                               I have all the information. Let me synthesis the information.
                                               </thought>
                                               <step_wise_result>
                                               The skills required for a software engineer include ......
                                               </step_wise_result>
                                               </example4>

                                               Make sure you include all information and artifacts in the response. DO NOT respond with a status update without the complete content.
                                               """ + 
                                               """
                                               ## ALWAYS Progress
                                               Once you plan to do something, progress with the plan immediately.
                                               DO NOT return a <tought> without any tool calls when the task is not complete and you are not awaiting any child agent's response.
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
        IGAgentFactory gAgentFactory,
        ILogger<PsiOmniGAgent> logger
    )
    {
        _kernelFactory = kernelFactory;
        _gAgentFactory = gAgentFactory;
        Logger = logger;
    }

    protected override async Task PerformConfigAsync(PsiOmniGAgentConfig configuration)
    {
        // First, call the base class method
        await base.PerformConfigAsync(configuration);

        // Initialize tracing after the grain is activated to ensure agent ID is available
        InitializeTracing();
        State.Name = configuration.Name;

        RaiseEventWithTracing(new InitializeEvent
        {
            Name = configuration.Name,
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
        var descriptionInfo = new AgentDescriptionInfo
        {
            Id = "PsiOmniGAgent",
            Name = "PsiOmni Integration Agent",
            L1Description = "AI agent for PsiOmni platform integration with advanced cognitive capabilities",
            L2Description = "Sophisticated PsiOmni platform agent that provides advanced AI cognitive services, neural network processing, and intelligent automation capabilities for complex problem-solving scenarios.",
            Category = "AI",
            Capabilities = new List<string> { "cognitive-services", "neural-processing", "intelligent-automation", "complex-problem-solving" },
            Tags = new List<string> { "psiomni", "cognitive", "ai", "automation" }
        };
        return Task.FromResult(JsonConverter.SerializeObject(descriptionInfo));
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
                Name = State.Name,
                AgentId = State.AgentId,
                AgentType = State.RealizationStatus == RealizationStatus.Orchestrator ? "orchestrator" : "specialized",
                Description = State.Description,
                Examples = State.Examples,
                Tools = State.Tools
            };

            var selfReportEvent = new SelfReportEvent
            {
                TargetAgentId = State.UserAgentId,
                SelfReport = selfReport
            };

            LogEventInfo(
                "Sending self report: UniqueId={UniqueId}, TargetAgent={TargetAgent}, AgentType={AgentType}, Description={Description}",
                selfReportEvent.UniqueId, State.UserAgentId, selfReport.AgentType, selfReport.Description);

            await PublishAsyncWithTracing(GrainId.Parse(State.UserAgentId), selfReportEvent);
        });
    }

    private async Task InitializeAsync()
    {
        LogEventDebug("Starting agent initialization for AgentId={AgentId}", AgentId);
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
            
            ## When deciding between "ORCHESTRATOR" and "SPECIALIZED"
            - Prefer SPECIALIZED mode if the agent's depth is more than 3
            - An agent with depth equal to 5 must operate in SPECIALIZED mode
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

        LogEventDebug("Executing GetChatMessageContent for initialization of AgentId={AgentId}", AgentId);
        var chatMessage = await ExecuteWithRetryAsync(
            async () => await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel),
            "GetChatMessageContent for initialization");
        LogEventDebug("GetChatMessageContent for initialization completed for AgentId={AgentId}", AgentId);
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
                LogEventDebug("Agent not initialized, skipping run");
                return;
            }

            LogEventInfo(
                "Starting agent run: Trigger={Trigger}, RealizationStatus={Status}, ChatHistoryLength={ChatLength}",
                trigger, State.RealizationStatus, State.ChatHistory.Count);

            Kernel kernel;
            ChatHistory chatHistory;
            int preHistoryLength;
            var systemPrompt = SystemPrompts[State.RealizationStatus];
            LogEventInfo("Status: {RealizationStatus}", State.RealizationStatus);
            LogEventDebug("Prompt: {SystemPrompt}",
                systemPrompt.Substring(0, Math.Min(100, systemPrompt.Length)) + "...");
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
            LogEventInfo("ChatHistory is empty.");
            return false;
        }

        if (State.Configuration == null)
        {
            LogEventInfo("Configuration is empty.");
            return false;
        }

        return true;
    }

    private ChatHistory GetChatHistory(string systemPrompt)
    {
        LogEventDebug("Building chat history with {MessageCount} messages", State.ChatHistory.Count);
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
                ex is HttpOperationException ||
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
                    LogEventError(ex, "Max retries ({MaxRetries}) reached for {Operation}. Last error: {Message}",
                        MaxRetries, operationName, ex.Message);
                    throw;
                }

                var errorType = "Timeout";
                if (ex is HttpOperationException httpEx)
                {
                    errorType = httpEx.StatusCode == HttpStatusCode.TooManyRequests
                        ? "Rate limit"
                        : "Other Http Operation Issue";
                }
                LogEventInfo(
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
            LogEventDebug("RunCoreAsync - Getting chat completion service");
            // 1. 获取 chat completion 服务
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

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
            LogEventDebug("RunCoreAsync - Execution settings configured");

            var chatHistory = GetChatHistory(systemPrompt);
            var preChatHistoryLength = chatHistory.Count;
            LogEventDebug("Chat history prepared: Length={Count}", preChatHistoryLength);

            var result = await ExecuteWithRetryAsync(
                async () => await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel),
                "GetChatMessageContent");

            chatHistory.Add(result);
            LogEventDebug("RunCoreAsync completed successfully");
            return (chatHistory, preChatHistoryLength);
        }
        catch (Exception e)
        {
            LogEventError(e, "Error during RunCoreAsync: {Message}", e.Message);
            throw;
        }
    }

    private async Task ReplyAsync(FinalResponse finalResult)
    {
        await TraceMethodAsync(async () =>
        {
            if (State.UserAgentId.IsNullOrEmpty())
            {
                var content = State.ChatHistory.Last()?.Content;
                if (content != null)
                {
                    if (content.Length <= 400)
                    {
                        LogEventInfo("Result:\n{Result}", content);
                    }
                    else
                    {
                        var firstPart = content.Substring(0, 200);
                        var lastPart = content.Substring(content.Length - 200);
                        LogEventInfo("Result (first 200 chars):\n{FirstPart}\n...\nResult (last 200 chars):\n{LastPart}", 
                            firstPart, lastPart);
                    }
                }
                LogEventDebug("No UserAgentId, logging result locally");
                return;
            }

            var agentMessageEvent = new AgentMessageEvent
            {
                TargetAgentId = State.UserAgentId,
                CallId = State.CallId,
                Content = finalResult.Response,
                Artifacts = finalResult.Artifacts,
                SenderAgentId = this.GetGrainId().ToString(),
                SenderAgentName = State.Name
            };

            LogEventInfo(
                "Sending reply: UniqueId={UniqueId}, TargetAgent={TargetAgent}, CallId={CallId}, ContentLength={Length}, ArtifactCount={ArtifactCount}",
                agentMessageEvent.UniqueId, State.UserAgentId, State.CallId, finalResult.Response?.Length ?? 0,
                finalResult.Artifacts.Count);

            await PublishAsyncWithTracing(GrainId.Parse(State.UserAgentId), agentMessageEvent);
        }, new { resultLength = finalResult.Response?.Length ?? 0 });
    }

    protected override void AIGAgentTransitionState(
        PsiOmniGAgentState state,
        StateLogEventBase<PsiOmniGAgentStateLogEvent> @event
    )
    {
        // Ensure tracing is initialized and scope exists
        InitializeTracing();

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
                state.Name = payload.Name;
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
                LogEventDebug("Updating agent configuration: AgentId={AgentId}, ParentAgentId={ParentAgentId}",
                    state.AgentId, payload.Event.ParentAgentId);
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
            {
                var content = $"Received reply from agent ({payload.Event.SenderAgentName}):\n\n{payload.Event.Content}";
                if (!payload.Event.Artifacts.IsNullOrEmpty())
                {
                    var artifacts = payload.Event.Artifacts.Select(
                        a => $"<artifact name=\"{a.Name}\" format=\"{a.Format}\">{a.Content}</artifact>"
                    ).JoinAsString("\n");
                    content += $"\n\n{artifacts}";
                }

                var amessage = PsiOmniChatMessage.CreateAssistantMessage(content);
                amessage.Metadata["CallId"] = payload.Event.CallId;
                state.ChatHistory.Add(amessage);
                var agentDescriptor = state.ChildAgents.Values.SingleOrDefault(a => a.AgentId == payload.Event.SenderAgentId);
                if(agentDescriptor != null)
                    state.AgentUsage.Remove(agentDescriptor.Name);
                ScheduleTask(async () =>
                {
                    LogEventDebug("Starting run due to Agent Message: {Content} with {ArtifactCount} artifacts", payload.Event.Content, payload.Event.Artifacts.Count);
                    await RunAsync($"Agent Message {payload.Event}");
                    LogEventDebug("Completed run due to Agent Message: {Content} with {ArtifactCount} artifacts", payload.Event.Content, payload.Event.Artifacts.Count);
                });
                break;
            }

            case NewAgentsCreatedEvent payload:
                LogEventInfo("New agents created: Count={Count}, AgentIds={AgentIds}",
                    payload.NewAgents.Count, string.Join(", ", payload.NewAgents.Select(a => a.AgentId)));

                foreach (var newAgent in payload.NewAgents)
                {
                    state.ChildAgents.TryAdd(newAgent.Name, newAgent);
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
            {
                if (payload.LastChildDescriptor.Name.IsNullOrEmpty())
                    break;
                    
                LogEventInfo("Updating child agent: AgentId={ChildAgentId}, AgentType={AgentType}",
                    payload.LastChildDescriptor.AgentId, payload.LastChildDescriptor.AgentType);
                AgentDescriptor? oldObj;
                // Child may proceed first and we receive this event before we process our own NewAgentsCreatedEvent event
                if (!state.ChildAgents.TryGetValue(payload.LastChildDescriptor.Name, out oldObj))
                {
                    oldObj = new AgentDescriptor()
                    {
                        Name = payload.LastChildDescriptor.Name,
                        AgentId = payload.LastChildDescriptor.AgentId
                    };
                    state.ChildAgents[payload.LastChildDescriptor.Name] = oldObj;
                }

                var oldObjClone = oldObj.DeepClone();
                var newObjClone = payload.LastChildDescriptor.DeepClone();
                oldObjClone.Examples = new List<AgentExample>();
                newObjClone.Examples = new List<AgentExample>();
                var refreshDescription = !oldObjClone.Equals(newObjClone);

                LogEventDebug("Child agent update: RefreshDescription={RefreshDescription}", refreshDescription);
                state.ChildAgents[payload.LastChildDescriptor.Name] = payload.LastChildDescriptor;
                ScheduleTask(async () =>
                {
                    if (refreshDescription)
                    {
                        await RunIntrospectionAsync();
                    }
                });
                break;
            }
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
                var finalResult = new FinalResponse();

                if (state.RealizationStatus == RealizationStatus.Specialized)
                {
                    finalResult.Response = State.ChatHistory.Last().Content;
                }
                else if (state.RealizationStatus == RealizationStatus.Orchestrator)
                {
                    var lastMessage = state.ChatHistory.Last()?.Content ?? string.Empty;
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

                        // Extract artifacts if present
                        if (lastMessage.Contains("<artifact"))
                        {
                            var artifactMatches = System.Text.RegularExpressions.Regex.Matches(
                                lastMessage,
                                @"<artifact name=""(.*?)"" format=""(.*?)"" />");

                            foreach (System.Text.RegularExpressions.Match match in artifactMatches)
                            {
                                var artifactName = match.Groups[1].Value.Trim();
                                var artifactFormat = match.Groups[2].Value.Trim();
                                if (State.Artifacts.TryGetValue(artifactName, out var artifact))
                                {
                                    finalResult.Artifacts.Add(new Artifact
                                    {
                                        Name = artifactName,
                                        Format = artifactFormat,
                                        Content = artifact.Content
                                    });
                                }
                            }
                        }

                        finalResult.Response = response;
                    }
                    catch (Exception ex)
                    {
                        finalResult.Response = lastMessage;
                        LogEventError(ex, "Failed to deserialize OrchestratorMessage: {Message}", lastMessage);
                    }
                }

                if (!finalResult.Response.IsNullOrEmpty())
                {
                    state.Examples.Last().Response = finalResult.Response;
                    ScheduleTask(async () =>
                    {
                        LogEventDebug("Starting report and reply: {Content}", finalResult.Response);
                        // TODO: Maybe update description.
                        await DoSelfReportAsync();
                        await ReplyAsync(finalResult);
                        LogEventDebug("Completed report and reply: {Content}", finalResult.Response);
                    });
                }
                else
                {
                    // Check for stuck state: agent returned thought without tool calls and no InProgress tasks
                    var lastMessage = state.ChatHistory.LastOrDefault();
                    var hasToolCalls = lastMessage?.ToolCalls?.Count > 0;
                    // Check if there are any InProgress tasks that are not assigned to this agent
                    var hasInProgressTasks = state.TodoList.Any(x => x.Status == TodoStatus.InProgress && x.AssigneeAgentId != this.GetGrainId().ToString());
                    var hasPendingTasks = state.TodoList.Any(x => x.Status == TodoStatus.Pending || (x.Status == TodoStatus.InProgress && x.AssigneeAgentId == this.GetGrainId().ToString()));
                    
                    // Extract thought to check if agent was thinking
                    var hasThought = false;
                    if (state.RealizationStatus == RealizationStatus.Orchestrator && lastMessage != null)
                    {
                        var content = lastMessage.Content ?? string.Empty;
                        hasThought = content.Contains("<thought>") && content.Contains("</thought>");
                    }
                    
                    // If the agent returned thought without tool calls, no InProgress tasks but has pending tasks, inject a <crank> message to continue processing.
                    if (hasThought && !hasToolCalls && !hasInProgressTasks && hasPendingTasks)
                    {
                        LogEventInfo("Detected stuck state: agent returned thought without tool calls, no InProgress tasks but has pending tasks. Injecting <crank> message to continue processing.");
                        
                        // Append crank message and schedule task run later
                        var crankMessage = PsiOmniChatMessage.CreateUserMessage("<crank>Continue processing the pending tasks.</crank>");
                        crankMessage.Metadata["IsCrank"] = "true";
                        state.ChatHistory.Add(crankMessage);
                        
                        // Schedule task run later
                        ScheduleTask(async () => await RunAsync("crank message continuation"));
                    }
                }

                break;
            case UpdateSelfDescription payload:
                LogEventInfo("Updating self description: NewDescription={Description}", payload.Description);
                state.Description = payload.Description;
                ScheduleTask(DoSelfReportAsync);
                break;
            case WriteTask payload:
                LogEventInfo("Writing task: Task={Task}", payload.Task);
                state.CurrentTask = payload.Task;
                break;
            case WriteDraftResponse payload:
                LogEventInfo("Writing draft response: Response={Response}", payload.DraftResponse);
                state.DraftResponse = payload.DraftResponse;
                break;
            case CallAgent payload:
                LogEventInfo("Calling agent: AgentName={}, TargetAgentId={TargetAgentId}, CallId={CallId}",
                    payload.AgentCall.AgentName,
                    payload.AgentCall.AgentId, payload.AgentCall.CallId);
                if (!state.AgentUsage.ContainsKey(payload.AgentCall.AgentName))
                {
                    state.AgentUsage.Add(payload.AgentCall.AgentName, payload.AgentCall.CallId);
                }

                break;
            case WriteArtifact payload:
                LogEventInfo("Writing artifact: Name={ArtifactName}, Format={Format}, ContentLength={ContentLength}",
                    payload.Name, payload.Format, payload.Content?.Length ?? 0);
                if (!state.Artifacts.ContainsKey(payload.Name))
                {
                    state.Artifacts.Add(payload.Name, new Artifact
                    {
                        Name = payload.Name,
                        Format = payload.Format,
                        Content = payload.Content
                    });
                }

                break;
        }

        LogEventDebug("State transition completed: EventType={EventType}",
            @event.GetType().Name);
    }
}