using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.LogEvent;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Newtonsoft.Json;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

public class WorkflowExecutionRecordGAgent :
    GAgentBase<WorkflowExecutionRecordState, WorkflowExecutionRecordLogEvent, EventBase>, IWorkflowExecutionRecordGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Workflow Execution Record GAgent");
    }

    [EventHandler]
    public async Task HandleEventAsync(StartExecuteWorkflowEvent @event)
    {
        RaiseEvent(new StartExecuteWorkflowLogEvent
        {
            WorkflowId = @event.WorkflowId,
            RoundId = @event.RoundId,
            WorkUnitInfos = @event.WorkUnitInfos,
            Content = @event.Content,
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(StartExecuteWorkUnitEvent @event)
    {
        RaiseEvent(new StartExecuteWorkUnitLogEvent
        {
            WorkUnitGrainId = @event.WorkUnitGrainId,
            InputData = JsonConvert.SerializeObject(@event.CoordinatorMessages)
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        RaiseEvent(new FinishExecuteWorkUnitLogEvent
        {
            WorkUnitGrainId = @event.PublisherGrainId.ToString(),
            OutputData = JsonConvert.SerializeObject(@event.ChatResponse?.Content)
        });
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(GroupChatFinishEvent @event)
    {
        RaiseEvent(new FinishExecuteWorkflowLogEvent
        {
        });
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(WorkflowExecutionRecordState state,
        StateLogEventBase<WorkflowExecutionRecordLogEvent> @event)
    {
        switch (@event)
        {
            case StartExecuteWorkflowLogEvent startExecuteWorkflowLogEvent:
                State.WorkflowId = startExecuteWorkflowLogEvent.WorkflowId;
                State.RoundId = startExecuteWorkflowLogEvent.RoundId;
                State.WorkUnitInfos = startExecuteWorkflowLogEvent.WorkUnitInfos;
                State.InitContent = startExecuteWorkflowLogEvent.Content;
                State.StartTime = DateTime.UtcNow;
                State.Status = WorkflowExecutionStatus.Running;
                break;
            case FinishExecuteWorkflowLogEvent finishExecuteWorkflowLogEvent:
                State.EndTime = DateTime.UtcNow;
                State.Status = WorkflowExecutionStatus.Completed;
                break;
            case StartExecuteWorkUnitLogEvent startExecuteWorkUnitLogEvent:
                State.WorkUnitRecords.Add(new WorkUnitExecutionRecord
                {
                    WorkUnitGrainId = startExecuteWorkUnitLogEvent.WorkUnitGrainId,
                    StartTime = DateTime.UtcNow,
                    Status = WorkflowExecutionStatus.Running,
                    InputData = startExecuteWorkUnitLogEvent.InputData
                });
                break;
            case FinishExecuteWorkUnitLogEvent finishExecuteWorkUnitLogEvent:
                var workUnit = State.WorkUnitRecords.FirstOrDefault(o =>
                    o.WorkUnitGrainId == finishExecuteWorkUnitLogEvent.WorkUnitGrainId);
                workUnit.EndTime = DateTime.UtcNow;
                workUnit.Status = WorkflowExecutionStatus.Completed;
                workUnit.OutputData = finishExecuteWorkUnitLogEvent.OutputData;
                break;
        }
    }
}

public interface IWorkflowExecutionRecordGAgent : IStateGAgent<WorkflowExecutionRecordState>
{
}