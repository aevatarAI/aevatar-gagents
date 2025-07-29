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
    [KernelFunction("call_new_agent")]
    [Description(
        "Creates a new agent with an initial task. The result will be notified to the given parent agent. Don't use call_agent to send the task again.")]
    public async Task<string> CreateAgentAsync(
        [Description("The ID of the parent agent.")]
        string parentAgentId,
        [Description("The call ID of this call.")]
        string callId,
        [Description("The description of the task sent to the new agent. Please provide all necessary information.")]
        string task
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

            // Create and initialize the new agent
            var psi = await _gAgentFactory.GetGAgentAsync("omni", "psi", new PsiOmniGAgentConfig()
            {
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

            var userMessageEvent = new UserMessageEvent
            {
                TargetAgentId = agentId.ToString(),
                CallId = callId,
                Content = task,
                ReplyToAgentId = parentAgentId
            };
            await PublishAsyncWithTracing(agentId, userMessageEvent);
            var descriptor = new AgentDescriptor
            {
                AgentId = agentId.ToString(),
                Examples = new List<AgentExample>()
                {
                    new AgentExample
                    {
                        Request = task
                    }
                }
            };

            LogEventInfo("Agent created successfully: AgentId={AgentId}, Depth={Depth}",
                agentId, State.Depth + 1);
            return
                $"Created the following agent and sent the subtask {callId} to it:\n{JsonSerializer.Serialize(descriptor)}";
        }
        catch (Exception ex)
        {
            var errorMessage = $"❌ Error creating agent for parent {parentAgentId}: {ex.Message}";
            Logger.LogError(ex, "❌ Error creating agent for parent {Parent}", parentAgentId);
            LogEventError(ex, "Failed to create agent: ParentAgent={ParentAgent}, CallId={CallId}",
                parentAgentId, callId);
            return errorMessage;
        }
    }

    [KernelFunction("call_agent")]
    [Description("Calls any ConfigurableAgentGrain by its ID with a natural language query")]
    public async Task<string> CallAgentAsync(
        [Description("The unique ID of the parent agent making the call.")]
        string parentAgentId,
        [Description("The unique ID of the agent to call."), Required]
        string agentId,
        [Description("The call ID of this call.")]
        string callId,
        [Description("Natural language message to be sent to the agent")]
        string message
    )
    {
        return await TraceMethodAsync(async () =>
        {
            Logger.LogInformation("🔗 Generic agent proxy called for {AgentId} with message: {Message}", agentId, message);
            LogEventInfo("Calling agent: ParentAgent={ParentAgent}, TargetAgent={TargetAgent}, CallId={CallId}, MessageLength={Length}",
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
    User: I want to add a dark mode toggle to the application settings. Make sure you run the tests and build when you're done!
    Assistant: I'll help add a dark mode toggle to your application settings. Let me create a todo list to track this implementation.
    *Creates todo list with the following items:*
    1. Create dark mode toggle component in Settings page
    2. Add dark mode state management (context/store)
    3. Implement CSS-in-JS styles for dark theme
    4. Update existing components to support theme switching
    5. Run tests and build process, addressing any failures or errors that occur
    *Begins working on the first task*

    <reasoning>
    The assistant used the todo list because:
    1. Adding dark mode is a multi-step feature requiring UI, state management, and styling changes
    2. The user explicitly requested tests and build be run afterward
    3. The assistant inferred that tests and build need to pass by adding ""Ensure tests and build succeed"" as the final task
    </reasoning>
    </example>

    <example>
    User: Help me rename the function getCwd to getCurrentWorkingDirectory across my project
    Assistant: Let me first search through your codebase to find all occurrences of 'getCwd'.
    *Uses grep or search tools to locate all instances of getCwd in the codebase*
    Assistant: I've found 15 instances of 'getCwd' across 8 different files. Let me create a todo list to track these changes.
    *Creates todo list with specific items for each file that needs updating*

    <reasoning>
    The assistant used the todo list because:
    1. First, the assistant searched to understand the scope of the task
    2. Upon finding multiple occurrences across different files, it determined this was a complex task with multiple steps
    3. The todo list helps ensure every instance is tracked and updated systematically
    4. This approach prevents missing any occurrences and maintains code consistency
    </reasoning>
    </example>


    <example>
    User: I need to implement these features for my e-commerce site: user registration, product catalog, shopping cart, and checkout flow.
    Assistant: I'll help implement these features. First, let's add all the features to the todo list.
    *Creates a todo list breaking down each feature into specific tasks based on the project architecture*
    Assistant: Let's start with user registration. This will involve creating database models, API endpoints, and frontend forms that integrate with your existing authentication system.

    <reasoning>
    The assistant used the todo list because:
    1. The user provided multiple complex features to implement in a comma separated list
    2. The todo list helps organize these large features into manageable tasks
    3. This approach allows for tracking progress across the entire implementation
    </reasoning>
    </example>

    <example>
    User: Can you help optimize my React application? It's rendering slowly and has performance issues.</user>
    Assistant: I'll help optimize your React application. First, let me examine your codebase to identify potential performance bottlenecks.
    *Reviews component structure, render patterns, state management, and data fetching*
    Assistant: After analyzing your codebase, I've identified several performance issues. Let me create a todo list to track our optimization efforts.
    *Creates todo list with items like: 1) Implement memoization for expensive calculations in ProductList, 2) Add virtualization for long lists in Dashboard, 3) Optimize image loading in Gallery component, 4) Fix state update loops in ShoppingCart, 5) Review bundle size and implement code splitting*
    Let's start by implementing memoization for the expensive calculations in your ProductList component.</assistant>

    <reasoning>
    The assistant used the todo list because:
    1. First, the assistant examined the codebase to identify specific performance issues
    2. Based on this analysis, it identified multiple optimization opportunities
    3. Performance optimization is a non-trivial task requiring multiple steps
    4. The todo list helps methodically track improvements across different components
    5. This systematic approach ensures all performance bottlenecks are addressed
    </reasoning>
    </example>

    ## Task States and Management

    1. **Task States**: Use these states to track progress:
       - Pending: Task not yet started
       - InProgress: Task is started but result is not received yet
       - Completed: Task finished successfully

    2. **Task Management**:
       - Update task status in real-time as you work
       - Mark tasks complete IMMEDIATELY after finishing (don't batch completions)
       - Remove tasks that are no longer relevant from the list entirely

    3. **Task Completion Requirements**:
       - ONLY mark a task as completed when you have FULLY accomplished it
       - If you encounter errors, blockers, or cannot finish, keep the task as InProgress
       - When blocked, create a new task describing what needs to be resolved

    4. **Task Breakdown**:
       - Create specific, actionable items
       - Break complex tasks into smaller, manageable steps
       - Use clear, descriptive task names

    When in doubt, use this tool. Being proactive with task management demonstrates attentiveness and ensures you complete all requirements successfully.
")
    ]
    public async Task<string> WriteTodosAsync(
        [Description("The updated list of todo items.")]
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