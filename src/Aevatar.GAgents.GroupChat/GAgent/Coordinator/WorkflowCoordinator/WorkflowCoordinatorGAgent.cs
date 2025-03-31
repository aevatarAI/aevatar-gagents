using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.LogEvent;
using GroupChat.GAgent.Feature.Blackboard;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

public class WorkflowCoordinatorGAgent : GAgentBase<WorkflowCoordinatorState, WorkflowCoordinatorLogEvent, EventBase,
    WorkflowCoordinatorConfigDto>, IWorkflowCoordinatorGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Workflow Coordinator GAgent");
    }

    #region EventHandler

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler ChatResponseEvent start");

        var workUnitInfo = State.GetWorkUnitFromTerm(@event.Term);
        if (workUnitInfo == null)
        {
            Logger.LogError($"[WorkflowCoordinatorGAgent] ChatResponseEvent can not fund term:{@event.Term}");
            return;
        }

        if (workUnitInfo.UnitStatusEnum != WorkerUnitStatusEnum.InProgress)
        {
            Logger.LogError(
                $"[WorkflowCoordinatorGAgent] ChatResponseEvent term status not correct term:{@event.Term}");
            return;
        }

        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(State.BlackboardId);
        await blackboard.SetMessageAsync(new CoordinatorConfirmChatResponse()
        {
            BlackboardId = @event.BlackboardId, MemberId = @event.MemberId, MemberName = @event.MemberName,
            ChatResponse = @event.ChatResponse
        });

        // maker sure this work unit has done
        RaiseEvent(new FinishedWorkUnitLogEvent() { Term = @event.Term, WorkUnitGrainId = workUnitInfo.GrainId });
        await ConfirmEvents();

        // indicate: no next work unit
        if (workUnitInfo.NextGrainId.IsNullOrEmpty())
        {
            await TryFinishWorkflowAsync();
            return;
        }

        await TryActiveWorkUnitAsync(workUnitInfo.NextGrainId);

        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler ChatResponseEvent end");
    }

    [EventHandler]
    public async Task HandleEventAsync(StartWorkflowCoordinatorEvent @event)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler StartWorkflowCoordinatorEvent start");
        if (State.WorkflowStatus != WorkflowCoordinatorStatus.Pending)
        {
            Logger.LogError(
                $"[WorkflowCoordinatorGAgent] handle StartWorkflowCoordinatorEvent error: State.WorkflowStatus != WorkflowCoordinatorStatus.Pending");
            return;
        }

        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(State.BlackboardId);
        await blackboard.ResetAsync();

        RaiseEvent(new WorkflowStartLogEvent());
        await ConfirmEvents();

        // try start top upstream work unit
        var topStreamGrainIds = State.GetTopUpStreamGrainIds();
        foreach (var item in topStreamGrainIds)
        {
            await TryActiveWorkUnitAsync(item, @event.InitContent);
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(ResetWorkflowEvent @event)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler ResetWorkflowEvent start");
        RaiseEvent(new ResetWorkflowLogEvent() { WorkflowUnit = @event.WorkflowUnitList });
        await ConfirmEvents();

        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler ResetWorkflowEvent end");
    }

    #endregion

    #region override method

    protected override async Task PerformConfigAsync(WorkflowCoordinatorConfigDto configuration)
    {
        Logger.LogDebug(
            $"[WorkflowCoordinatorGAgent] [PerformConfigAsync] WorkflowCoordinatorConfigDto:{JsonConvert.SerializeObject(configuration)}");

        RaiseEvent(new InitWorkflowCoordinatorLogEvent
            { WorkflowUnit = configuration.WorkflowUnitList, BlackBoardId = configuration.BlackBoardId });

        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(WorkflowCoordinatorState state,
        StateLogEventBase<WorkflowCoordinatorLogEvent> @event)
    {
        switch (@event)
        {
            case InitWorkflowCoordinatorLogEvent initWorkflowCoordinatorLogEvent:
                var nodeList = initWorkflowCoordinatorLogEvent.WorkflowUnit.Select(s => new WorkUnitInfo()
                {
                    GrainId = s.GrainId,
                    NextGrainId = s.NextGrainId,
                    UnitStatusEnum = WorkerUnitStatusEnum.Pending,
                }).ToList();

                State.CurrentWorkUnitInfos = nodeList;
                state.BlackboardId = initWorkflowCoordinatorLogEvent.BlackBoardId;
                break;

            case FinishedWorkUnitLogEvent finishedWorkUnitLogEvent:
                var workUnitInfo =
                    State.CurrentWorkUnitInfos.First(f => f.GrainId == finishedWorkUnitLogEvent.WorkUnitGrainId);
                workUnitInfo.UnitStatusEnum = WorkerUnitStatusEnum.Finished;
                State.TermToWorkUnitGrainId.Remove(finishedWorkUnitLogEvent.Term);
                break;

            case WorkflowFinishLogEvent workflowFinishLogEvent:
                State.WorkflowStatus = WorkflowCoordinatorStatus.Pending;
                State.TermToWorkUnitGrainId = new Dictionary<int, string>();
                if (State.BackupWorkUnitInfos.Count > 0)
                {
                    State.CurrentWorkUnitInfos = State.BackupWorkUnitInfos.Select(s=>s).ToList();
                    State.BackupWorkUnitInfos.Clear();
                }
                else
                {
                    for (var i = 0; i < State.CurrentWorkUnitInfos.Count; i++)
                    {
                        var workUnit = State.CurrentWorkUnitInfos[i];
                        workUnit.UnitStatusEnum = WorkerUnitStatusEnum.Pending;
                    }
                }

                break;

            case StartWorkUnitLogEvent workUnitLogEvent:
                var startWorkUnitInfo =
                    State.CurrentWorkUnitInfos.First(f => f.GrainId == workUnitLogEvent.WorkUnitGrainId);
                startWorkUnitInfo.UnitStatusEnum = WorkerUnitStatusEnum.InProgress;
                State.TermToWorkUnitGrainId.Add(workUnitLogEvent.Term, workUnitLogEvent.WorkUnitGrainId);
                State.Term += 1;
                break;

            case WorkflowStartLogEvent workflowStartLogEvent:
                State.WorkflowStatus = WorkflowCoordinatorStatus.InProgress;
                break;

            case ResetWorkflowLogEvent resetWorkflowLogEvent:
                var backupNodeList = resetWorkflowLogEvent.WorkflowUnit.Select(s => new WorkUnitInfo()
                {
                    GrainId = s.GrainId,
                    NextGrainId = s.NextGrainId,
                    UnitStatusEnum = WorkerUnitStatusEnum.Pending,
                }).ToList();
                if (State.WorkflowStatus == WorkflowCoordinatorStatus.Pending)
                {
                    State.CurrentWorkUnitInfos = backupNodeList;
                }
                else
                {
                    State.BackupWorkUnitInfos = backupNodeList;
                }

                break;
        }
    }

    #endregion

    #region private method

    private async Task TryFinishWorkflowAsync()
    {
        if (State.WorkflowStatus != WorkflowCoordinatorStatus.InProgress)
        {
            return;
        }

        if (State.CheckAllWorkUnitFinished())
        {
            await PublishAsync(new GroupChatFinishEvent() { BlackboardId = State.BlackboardId });
            RaiseEvent(new WorkflowFinishLogEvent());
            await ConfirmEvents();
        }
    }

    private async Task TryActiveWorkUnitAsync(string workUnitGrainId, string? content = null)
    {
        Logger.LogDebug($"[WorkflowCoordinatorGAgent] Active work:{workUnitGrainId} start");
        if (State.CheckWorkUnitCanProgress(workUnitGrainId) == false)
        {
            Logger.LogDebug($"[WorkflowCoordinatorGAgent] Active work:{workUnitGrainId} fail");
            return;
        }

        var upstreamGrains = State.GetUpStreamGrainIds(workUnitGrainId).Select(s => GrainId.Parse(s).GetGuidKey());
        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(State.BlackboardId);
        var messages = await blackboard.GetLastChatMessageAsync(upstreamGrains.ToList());
        if (content != null)
        {
            messages.Add(new ChatMessage() { MessageType = MessageType.BlackboardTopic, Content = content });
        }

        await PublishAsync(new ChatEvent()
        {
            BlackboardId = State.BlackboardId, Speaker = GrainId.Parse(workUnitGrainId).GetGuidKey(), Term = State.Term,
            CoordinatorMessages = messages
        });

        RaiseEvent(new StartWorkUnitLogEvent() { WorkUnitGrainId = workUnitGrainId, Term = State.Term });
        await ConfirmEvents();

        Logger.LogDebug($"[WorkflowCoordinatorGAgent] Active work:{workUnitGrainId} end");
    }

    #endregion
}

public interface IWorkflowCoordinatorGAgent : IStateGAgent<WorkflowCoordinatorState>
{
}