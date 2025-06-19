using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using GroupChat.GAgent;
using GroupChat.GAgent.Dto;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Microsoft.Extensions.Logging;

namespace SimpleAIWorkflow.Grains;

/// <summary>
/// AI workflow node state, inherits from GroupMemberState
/// </summary>
[GenerateSerializer]
public class WorkflowAIAgentState : GroupMemberState
{
    [Id(0)] public string WorkflowType { get; set; } = "standard";
    [Id(1)] public string TaskDescription { get; set; } = "AI workflow task";
    [Id(2)] public int Priority { get; set; } = 50;
    [Id(3)] public int TimeoutMilliseconds { get; set; } = 30000;
}

/// <summary>
/// AI workflow node - inherits from GroupMemberGAgentBase, supports AI conversation and intelligent decision making
/// </summary>
public class WorkflowAIAgent : GroupMemberGAgentBase<WorkflowAIAgentState, WorkflowAIAgentEventLog, EventBase, WorkflowAIAgentConfigDto>, IWorkflowAIAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult($"WorkflowAIAgent: {State.MemberName} - Intelligent workflow processing agent");
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // Calculate interest value based on workflow configuration and current task priority
        var baseInterest = State.Priority;
        
        // Add some randomness to simulate AI judgment uncertainty
        var random = new Random();
        var adjustment = random.Next(-10, 10);
        
        return Task.FromResult(Math.Max(1, Math.Min(100, baseInterest + adjustment)));
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? messages)
    {
        var response = new ChatResponse();
        
        // Build AI workflow response
        var taskDescription = State.TaskDescription;
        var processingResult = await ProcessWorkflowTaskAsync(messages, taskDescription);
        
        response.Content = $"🤖 {State.MemberName}: {processingResult}";
        
        Logger.LogInformation($"WorkflowAIAgent {State.MemberName} processing task: {taskDescription}");
        Console.WriteLine($"🤖 {State.MemberName} executing AI workflow task");
        Console.WriteLine($"   📝 Task description: {taskDescription}");
        Console.WriteLine($"   💬 Input messages: {string.Join(", ", messages?.Select(m => m.Content) ?? new List<string>())}");
        Console.WriteLine($"   ✅ Processing result: {processingResult}");
        
        return response;
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        Logger.LogInformation($"WorkflowAIAgent {State.MemberName} completed workflow task: {State.TaskDescription}");
        Console.WriteLine($"🏁 {State.MemberName} workflow task completed");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Core logic for processing AI workflow tasks
    /// </summary>
    private async Task<string> ProcessWorkflowTaskAsync(List<ChatMessage>? messages, string taskDescription)
    {
        // Simulate AI processing
        await Task.Delay(100); // Simulate AI thinking time
        
        var workflowType = State.WorkflowType;
        
        switch (workflowType.ToLower())
        {
            case "approval":
                return ProcessApprovalWorkflow(messages, taskDescription);
            case "analysis":
                return ProcessAnalysisWorkflow(messages, taskDescription);
            case "validation":
                return ProcessValidationWorkflow(messages, taskDescription);
            default:
                return ProcessStandardWorkflow(messages, taskDescription);
        }
    }

    private string ProcessApprovalWorkflow(List<ChatMessage>? messages, string taskDescription)
    {
        var random = new Random();
        var approved = random.Next(1, 100) > 20; // 80% approval rate
        
        return approved 
            ? $"✅ Approval passed: {taskDescription} - Verified and approved for execution"
            : $"❌ Approval rejected: {taskDescription} - Requires further review";
    }

    private string ProcessAnalysisWorkflow(List<ChatMessage>? messages, string taskDescription)
    {
        var random = new Random();
        var riskScore = random.Next(1, 100);
        var riskLevel = riskScore switch
        {
            < 30 => "Low Risk",
            < 70 => "Medium Risk", 
            _ => "High Risk"
        };
        
        return $"📊 Analysis result: {taskDescription} - Risk score: {riskScore}/100 ({riskLevel})";
    }

    private string ProcessValidationWorkflow(List<ChatMessage>? messages, string taskDescription)
    {
        var random = new Random();
        var validationPassed = random.Next(1, 100) > 15; // 85% pass rate
        
        return validationPassed
            ? $"✅ Validation passed: {taskDescription} - Data integrity and format meet requirements"
            : $"⚠️ Validation failed: {taskDescription} - Data anomalies detected, correction needed";
    }

    private string ProcessStandardWorkflow(List<ChatMessage>? messages, string taskDescription)
    {
        return $"🔄 Processed: {taskDescription} - Standard workflow processing completed, results recorded";
    }

    /// <summary>
    /// Configuration event - Set workflow type
    /// </summary>
    [GenerateSerializer]
    public class SetWorkflowTypeLogEvent : StateLogEventBase<WorkflowAIAgentEventLog>
    {
        [Id(0)] public string WorkflowType { get; set; } = "standard";
    }

    /// <summary>
    /// Configuration event - Set task description
    /// </summary>
    [GenerateSerializer]
    public class SetTaskDescriptionLogEvent : StateLogEventBase<WorkflowAIAgentEventLog>
    {
        [Id(0)] public string TaskDescription { get; set; } = "";
    }

    /// <summary>
    /// Configuration event - Set priority
    /// </summary>
    [GenerateSerializer]
    public class SetPriorityLogEvent : StateLogEventBase<WorkflowAIAgentEventLog>
    {
        [Id(0)] public int Priority { get; set; } = 50;
    }

    /// <summary>
    /// Configuration event - Set timeout
    /// </summary>
    [GenerateSerializer]
    public class SetTimeoutLogEvent : StateLogEventBase<WorkflowAIAgentEventLog>
    {
        [Id(0)] public int TimeoutMilliseconds { get; set; } = 30000;
    }

    protected override async Task PerformConfigAsync(WorkflowAIAgentConfigDto configuration)
    {
        // Call base configuration method to handle MemberName
        await base.PerformConfigAsync(configuration);
        
        // Handle WorkflowAIAgent specific configuration
        RaiseEvent(new SetWorkflowTypeLogEvent() { WorkflowType = configuration.WorkflowType });
        RaiseEvent(new SetTaskDescriptionLogEvent() { TaskDescription = configuration.TaskDescription });
        RaiseEvent(new SetPriorityLogEvent() { Priority = configuration.Priority });
        RaiseEvent(new SetTimeoutLogEvent() { TimeoutMilliseconds = configuration.TimeoutMilliseconds });
        
        await ConfirmEvents();
    }

    protected override void GroupMemberTransitionState(WorkflowAIAgentState state, StateLogEventBase<WorkflowAIAgentEventLog> @event)
    {
        switch (@event)
        {
            case SetWorkflowTypeLogEvent setWorkflowTypeLogEvent:
                State.WorkflowType = setWorkflowTypeLogEvent.WorkflowType;
                break;
            case SetTaskDescriptionLogEvent setTaskDescriptionLogEvent:
                State.TaskDescription = setTaskDescriptionLogEvent.TaskDescription;
                break;
            case SetPriorityLogEvent setPriorityLogEvent:
                State.Priority = setPriorityLogEvent.Priority;
                break;
            case SetTimeoutLogEvent setTimeoutLogEvent:
                State.TimeoutMilliseconds = setTimeoutLogEvent.TimeoutMilliseconds;
                break;
        }
    }
}

/// <summary>
/// WorkflowAIAgent interface definition
/// </summary>
public interface IWorkflowAIAgent : IAIGAgent, IGAgent
{
}

/// <summary>
/// WorkflowAIAgent configuration data transfer object
/// </summary>
[GenerateSerializer]
public class WorkflowAIAgentConfigDto : GroupMemberConfigDto
{
    /// <summary>
    /// Workflow type (standard, approval, analysis, validation)
    /// </summary>
    public string WorkflowType { get; set; } = "standard";
    
    /// <summary>
    /// Task description
    /// </summary>
    public string TaskDescription { get; set; } = "AI workflow task";
    
    /// <summary>
    /// Priority (1-100)
    /// </summary>
    public int Priority { get; set; } = 50;
    
    /// <summary>
    /// Timeout (milliseconds)
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 30000;
}

/// <summary>
/// WorkflowAIAgent event log
/// </summary>
[GenerateSerializer]
public class WorkflowAIAgentEventLog : StateLogEventBase<WorkflowAIAgentEventLog>
{
    /// <summary>
    /// Processed task type
    /// </summary>
    public string TaskType { get; set; } = "";
    
    /// <summary>
    /// Processing result
    /// </summary>
    public string ProcessingResult { get; set; } = "";
    
    /// <summary>
    /// Processing timestamp
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
} 