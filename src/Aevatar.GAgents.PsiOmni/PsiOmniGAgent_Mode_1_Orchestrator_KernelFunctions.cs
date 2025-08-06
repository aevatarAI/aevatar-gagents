using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    /// <summary>
    /// Create a new specialized agent with custom prompt and tools
    /// </summary>
    [KernelFunction("query_existing_agents")]
    [Description("Query what agents are available.")]
    public async Task<List<AgentWithUsage>> QueryAgentsAsync()
    {
        var result = State.ChildAgents.Values.Select(x => new AgentWithUsage
        {
            AgentId = x.AgentId,
            AgentType = x.AgentType,
            Description = x.Description,
            Examples = x.Examples,
            Tools = x.Tools,
            HandlingTask = State.AgentUsage.GetOrDefault(x.AgentId) ?? string.Empty
        }).ToList();
        return await Task.FromResult(result);
    }

    [KernelFunction("write_artifact")]
    [Description("Write an artifact.")]
    public async Task<string> WriteArtifactAsync(
        [Description("The name of the artifact. It has to be unique and must be a valid file name with a valid extension."), Required]
        string name,
        [Description("The format of the artifact. It has to be a valid file extension."), Required]
        string format,
        [Description("The content of the artifact."), Required]
        string content
    )
    {
        if (State.Artifacts.ContainsKey(name))
        {
            return await Task.FromResult<string>("Failed to write artifact: name {name} exits. Pick another name.");
        }

        State.Artifacts.TryAdd(name, new Artifact
        {
            Name = name,
            Format = format,
            Content = content
        });
        RaiseEventWithTracing(new WriteArtifact
        {
            Name = name,
            Format = format,
            Content = content
        });
        return await Task.FromResult("Written artifact.");
    }
    
    [KernelFunction("write_task")]
    [Description("Rewrite the current task.")]
    public async Task<string> WriteTaskAsync(
        [Description("The comprehensive description of the task."), Required]
        string task
    )
    {
        RaiseEventWithTracing(new WriteTask()
        {
            Task = task
        });
        return await Task.FromResult("Written task.");
    }

    [KernelFunction("read_task")]
    [Description("Read the current task.")]
    public async Task<string> ReadTaskAsync(
    )
    {
        return await Task.FromResult(State.CurrentTask);
    }

    [KernelFunction("response_draft_write")]
    [Description("Write the draft response.")]
    public async Task<string> WriteResponseProposalAsync(
        [Description("The draft response."), Required]
        string draftResponse
    )
    {
        RaiseEventWithTracing(new WriteDraftResponse()
        {
            DraftResponse = draftResponse
        });
        return await Task.FromResult("Written draft response.");
    }
    
    /// <summary>
    /// Create a new specialized agent with custom prompt and tools
    /// </summary>
    [KernelFunction("create_agent")]
    [Description("Creates a new agent.")]
    public async Task<string> CreateAgentAsync(
        [Description("The ID of the parent agent.")]
        string parentAgentId,
        [Description(
            "The description of the agent. Include all necessary information such as the persona, knowledge and capabilities.")]
        string description,
        [Description("The example tasks that the agent can perform.")]
        string exampleTasks
    )
    {
        try
        {
            // Create agent configuration with custom prompt
            var agentConfig = State.Configuration;
            if (agentConfig == null)
            {
                return
                    $"❌ Error: AgentConfiguration not set.";
            }

            if (State.Depth >= 5)
            {
                return "❌ Error: Can't create agent with depth > 5.";
            }

            // Create and initialize the new agent
            var psi = await _gAgentFactory.GetGAgentAsync("omni", "psi", new PsiOmniGAgentConfig()
            {
                ParentId = parentAgentId,
                Description = description,
                Examples = exampleTasks,
                Depth = State.Depth + 1
            });
            var agentId = psi.GetGrainId();
            // There's a publisher tied to each parent agent.
            var configEvent = new AgentConfigEvent
            {
                Configuration = agentConfig,
                ParentAgentId = parentAgentId
            };
            await PublishAsyncWithTracing(agentId, configEvent);

            var descriptor = new AgentDescriptor
            {
                AgentId = agentId.ToString(),
                Description = description
            };

            RaiseEventWithTracing(new AddNewAgent()
            {
                NewAgent = descriptor
            });

            LogEventInfo("Agent created successfully: AgentId={AgentId}, Depth={Depth}",
                agentId, State.Depth + 1);
            return $"Created the following agent:\n{JsonSerializer.Serialize(descriptor)}";
        }
        catch (Exception ex)
        {
            var errorMessage = $"❌ Error creating agent for parent {parentAgentId}: {ex.Message}";
            Logger.LogError(ex, "❌ Error creating agent for parent {Parent}", parentAgentId);
            return errorMessage;
        }
    }

    [KernelFunction("call_agent")]
    [Description("Calls any ConfigurableAgentGrain by its ID with a natural language query")]
    public async Task<string> CallAgentAsync(
        [Description("The unique ID of the agent to call."), Required]
        string agentId,
        [Description("The call ID of this call.")]
        string callId,
        [Description("The task to be sent.")] TaskDispatch task
    )
    {
        var message = $"Task: {task.Task}\n\nBackground: {task.Background}";
        if (!task.Knowledge.IsNullOrEmpty())
        {
            var knowledge = task.Knowledge.Select(x => $"<knowledge>{x}</knowledge>").JoinAsString("\n");
            message += $"\n\nKnowledge:\n{knowledge}";
        }

        var parentAgentId = this.GetGrainId().ToString();
        if (parentAgentId == agentId)
        {
            return "Failed to call agent: Calling self is disallowed.";
        }

        return await TraceMethodAsync(async () =>
        {
            if (State.AgentUsage.TryGetValue(agentId, out var anotherCallId))
            {
                Logger.LogWarning("Agent {AgentId} is in use. Hanlding another call: {CallId}", agentId, anotherCallId);
                return "Failed to call agent. Agent is handling another call.";
            }
            Logger.LogInformation("🔗 Generic agent proxy called for {AgentId} with message: {Message}", agentId,
                message);
            LogEventInfo(
                "Calling agent: ParentAgent={ParentAgent}, TargetAgent={TargetAgent}, CallId={CallId}, MessageLength={Length}",
                parentAgentId, agentId, callId, message?.Length ?? 0);

            try
            {
                var targetAgent = await _gAgentFactory.GetGAgentAsync(GrainId.Parse(agentId));

                var userMessageEvent = new UserMessageEvent
                {
                    TargetAgentId = agentId,
                    CallId = callId,
                    Content = message,
                    ReplyToAgentId = parentAgentId
                };
                await PublishAsyncWithTracing(GrainId.Parse(agentId), userMessageEvent);

                var call = new AgentCall
                {
                    AgentId = agentId,
                    CallId = callId,
                    Message = message
                };

                LogEventInfo("Agent call sent successfully: TargetAgent={TargetAgent}, CallId={CallId}",
                    agentId, callId);
                State.AgentUsage.TryAdd(agentId, callId);
                RaiseEventWithTracing(new CallAgent()
                {
                    AgentCall = call
                });
                return $"Agent call sent: {JsonSerializer.Serialize(call)}";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ Error in generic agent proxy for {AgentId}", agentId);
                LogEventError(ex, "Failed to call agent: TargetAgent={TargetAgent}, CallId={CallId}",
                    agentId, callId);
                return $"Error calling agent '{agentId}': {ex.Message}";
            }
        }, new { parentAgentId, agentId, callId, messageLength = message?.Length });
    }

    [KernelFunction("todo_read")]
    [Description(
        @"Read the current todo list.

Use this tool to read the current to-do list for the session. This tool should be used proactively and frequently to ensure that you are aware of
the status of the current task list. You should make use of this tool as often as possible, especially in the following situations:
    - At the beginning of conversations to see what's pending
    - Before starting new tasks to prioritize work
    - When the user asks about previous tasks or plans
    - Whenever you're uncertain about what to do next
    - After completing tasks to update your understanding of remaining work
    - After every few messages to ensure you're on track

    Usage:
    - This tool takes in no parameters. So leave the input blank or empty. DO NOT include a dummy object, placeholder string or a key like ""input"" or ""empty"". LEAVE IT BLANK.
    - Returns a list of todo items with their status, priority, and content
    - Use this information to track progress and plan next steps
    - If no todos exist yet, an empty list will be returned"
    )]
    public async Task<IReadOnlyList<TodoItem>> ReadTodosAsync()
    {
        return await Task.FromResult(State.TodoList);
    }

    [KernelFunction("todo_write")]
    [Description(
        @"Update the todo list for the current session. To be used proactively and often to track progress and pending tasks.
Use this tool to create and manage a structured task list for your current coding session. This helps you track progress, organize complex tasks, and demonstrate thoroughness to the user.
    It also helps the user understand the progress of the task and overall progress of their requests.

    ## When to Use This Tool
    Use this tool proactively in these scenarios:

    1. Task planning - When a task needs to be broken down
    2. User explicitly requests todo list - When the user directly asks you to use the todo list
    3. User provides multiple tasks - When users provide a list of things to be done (numbered or comma-separated)
    4. After receiving new instructions - Immediately capture user requirements as todos
    5. When you start working on a task - Mark it as in_progress after delegating a task to a child agent
    6. After receiving a callback from a child task that completes it - Mark it as completed and add any new follow-up tasks discovered during implementation

    ## When NOT to Use This Tool

    Skip using this tool when:
    1. The task is purely conversational or informational

    ## Examples of When to Use the Todo List

    <example>
    User: Research on machine learning techniques and prepare an html format cheatsheet for me
    Assistant: Let me create a todo list.
    *Creates todo list with the following items:*
    1. Research on popular and useful machine learning techniques
    2. Use the research result from todo item 1 and create an html (dependency on todo item 1)
    </example>

    ## Task States and Management

    1. **Task States**: Use these states to track progress:
       - Pending: Task not yet started
       - InProgress: Task is started but result is not received yet
       - Completed: Task finished successfully

    2. **Task Management**:
       - Update task status in real-time as you work
       - Mark tasks complete IMMEDIATELY after receiving the result from the agent handling it even if the handling agent returns an unsuccessful result
       - Create a new todo item for retry or rephrased tasks
       - Retain all tasks until the main task is fully completed

    3. **Task Breakdown**:
       - Create specific, actionable items
       - Break complex tasks into smaller, manageable steps
       - Use clear, descriptive task names

    ## Requirements for Input Data
    Todo items must have an id assigned to it (use a running integer as the id).
    InProgress and Completed todo items must have the AssigneeAgentId.

    When in doubt, use this tool. Being proactive with task management demonstrates attentiveness and ensures you complete all requirements successfully.
")
    ]
    public async Task<string> WriteTodosAsync(
        [Description(
            "The updated list of todo items. Please supply the full list as this operation overwrites all data.")]
        List<TodoItem> updatedTodos
    )
    {
        LogEventInfo("Updating todo list: OldCount={OldCount}, NewCount={NewCount}, Changes={Changes}",
            State.TodoList.Count, updatedTodos.Count,
            GetTodoChanges(State.TodoList, updatedTodos));

        State.TodoList = updatedTodos;
        RaiseEventWithTracing(new UpdateTodoList()
        {
            Todos = updatedTodos
        });
        // await ConfirmEventsWithTracing();
        return "Successfully updated todo list";
    }

    private string GetTodoChanges(List<TodoItem> oldList, List<TodoItem> newList)
    {
        var changes = new List<string>();
        var newIds = newList.Select(t => t.Id).ToHashSet();
        var oldIds = oldList.Select(t => t.Id).ToHashSet();

        // Find new todos
        var added = newIds.Except(oldIds).Count();
        if (added > 0) changes.Add($"{added} added");

        // Find removed todos
        var removed = oldIds.Except(newIds).Count();
        if (removed > 0) changes.Add($"{removed} removed");

        // Find status changes
        var statusChanges = 0;
        foreach (var newTodo in newList)
        {
            var oldTodo = oldList.FirstOrDefault(t => t.Id == newTodo.Id);
            if (oldTodo != null && oldTodo.Status != newTodo.Status)
            {
                statusChanges++;
            }
        }

        if (statusChanges > 0) changes.Add($"{statusChanges} status changes");

        return changes.Count > 0 ? string.Join(", ", changes) : "no changes";
    }
}