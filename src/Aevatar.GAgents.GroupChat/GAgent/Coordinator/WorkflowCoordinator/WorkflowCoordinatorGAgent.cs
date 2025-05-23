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

[GAgent]
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

        var downStreamList = State.GetDownStreamGrainIds(workUnitInfo.GrainId);
        // indicate: no next work unit
        if (downStreamList.Count == 0)
        {
            await TryFinishWorkflowAsync();
            return;
        }

        foreach (var grainId in downStreamList)
        {
            await TryActiveWorkUnitAsync(grainId);
        }

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

        if (State.BlackboardId == Guid.Empty)
        {
            Logger.LogError(
                $"[WorkflowCoordinatorGAgent] BlackboardId is not init");
            return;
        }
        
        if (!State.CurrentWorkUnitInfos.Any())
        {
            Logger.LogError(
                $"[WorkflowCoordinatorGAgent] Work unit is not init");
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

        var blackBoardId = this.GetPrimaryKey();
        var blackboardAgent = GrainFactory.GetGrain<IBlackboardGAgent>(blackBoardId);
        await RegisterAsync(blackboardAgent);

        RaiseEvent(new InitWorkflowCoordinatorLogEvent
            { WorkflowUnit = configuration.WorkflowUnitList, BlackBoardId = blackBoardId });

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
                var workUnitInfoList =
                    State.CurrentWorkUnitInfos.FindAll(f => f.GrainId == finishedWorkUnitLogEvent.WorkUnitGrainId);
                for (int i = 0; i < workUnitInfoList.Count; i++)
                {
                    var workUnit = workUnitInfoList[i];
                    workUnit.UnitStatusEnum = WorkerUnitStatusEnum.Finished;
                }

                State.TermToWorkUnitGrainId.Remove(finishedWorkUnitLogEvent.Term);
                break;

            case WorkflowFinishLogEvent workflowFinishLogEvent:
                State.WorkflowStatus = WorkflowCoordinatorStatus.Pending;
                State.TermToWorkUnitGrainId = new Dictionary<int, string>();
                if (State.BackupWorkUnitInfos.Count > 0)
                {
                    State.CurrentWorkUnitInfos = State.BackupWorkUnitInfos.Select(s => s).ToList();
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
                var startWorkUnitInfoList =
                    State.CurrentWorkUnitInfos.FindAll(f => f.GrainId == workUnitLogEvent.WorkUnitGrainId);
                for (var i = 0; i < startWorkUnitInfoList.Count; i++)
                {
                    var startWorkUnitInfo = startWorkUnitInfoList[i];
                    startWorkUnitInfo.UnitStatusEnum = WorkerUnitStatusEnum.InProgress;
                }

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
            var grainIdList = TentativeState.GetAllWorkerUnitGrainIds();
            foreach (var grainId in grainIdList)
            {
                var speaker = GrainId.Parse(grainId);
                await PublishP2PAsync(speaker, new GroupChatFinishEvent()
                {
                    BlackboardId = State.BlackboardId
                });
            }

            // await PublishAsync(new GroupChatFinishEvent() { BlackboardId = State.BlackboardId });
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

        var speaker = GrainId.Parse(workUnitGrainId);
        await PublishP2PAsync(speaker, new ChatEvent()
        {
            BlackboardId = State.BlackboardId, Speaker = speaker.GetGuidKey(), Term = State.Term,
            CoordinatorMessages = messages
        });

        RaiseEvent(new StartWorkUnitLogEvent() { WorkUnitGrainId = workUnitGrainId, Term = State.Term });
        await ConfirmEvents();

        Logger.LogDebug($"[WorkflowCoordinatorGAgent] Active work:{workUnitGrainId} end");
    }

    private async Task<bool> TryRegisterWorkUnitsAsync(List<WorkflowUnitDto> workflowUnits)
    {

        var workflowUnitGrains = new Dictionary<string, IGAgent>();
        foreach (var unit in workflowUnits)
        {
            
            
            
            var grainId = GrainId.Parse(unit.GrainId);
            var agent = GrainFactory.GetGrain<IGAgent>(grainId);
            
            // TODO: check is GroupMemberGAgentBase<,,,>
            
            var agentParent = await agent.GetParentAsync();
            if (agentParent != default && agentParent != this.GetGrainId())
            {
                return false;
            }
            
            workflowUnitGrains.Add(unit.GrainId, agent);
        }

        if (workflowUnitGrains.Count == 0)
        {
            return false;
        }

        foreach (var item in workflowUnitGrains.Values)
        {
            await RegisterAsync(item);
        }

        return true;
    }
    
    public bool IsAllPathsCanReachTerminal(List<WorkflowUnitDto> workflowUnits)
    {
        Dictionary<string, List<string>> graph = new();
        HashSet<string> allNodeIds = new();

        foreach (var unit in workflowUnits)
        {
            allNodeIds.Add(unit.GrainId);
            if (!graph.ContainsKey(unit.GrainId))
                graph[unit.GrainId] = new List<string>();
        
            if (!string.IsNullOrWhiteSpace(unit.NextGrainId))
            {
                graph[unit.GrainId].Add(unit.NextGrainId);
                allNodeIds.Add(unit.NextGrainId); 
            }
        }
        
        var terminalNodes = workflowUnits
            .Where(n => string.IsNullOrWhiteSpace(n.NextGrainId))
            .Select(n => n.GrainId)
            .ToHashSet();
        
        var reachable = new HashSet<string>(terminalNodes);
        var queue = new Queue<string>(terminalNodes);
        
        Dictionary<string, List<string>> reverseGraph = new();

        foreach (var unit in workflowUnits)
        {
            if (!string.IsNullOrWhiteSpace(unit.NextGrainId))
            {
                if (!reverseGraph.ContainsKey(unit.NextGrainId))
                    reverseGraph[unit.NextGrainId] = new List<string>();
                reverseGraph[unit.NextGrainId].Add(unit.GrainId);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (reverseGraph.TryGetValue(current, out var preNodes))
            {
                foreach (var node in preNodes)
                {
                    if (!reachable.Contains(node))
                    {
                        reachable.Add(node);
                        queue.Enqueue(node);
                    }
                }
            }
        }
        
        foreach (var nodeId in allNodeIds)
        {
            if (!reachable.Contains(nodeId))
                return false; 
        }

        return true;
    }

    #endregion

    #region protected method

    protected async Task PublishP2PAsync<T>(GrainId grainId, T @event) where T : EventBase
    {
        var grainIdString = grainId.ToString();
        var streamId = StreamId.Create(AevatarOptions!.StreamNamespace,
            grainIdString);
        var stream = StreamProvider.GetStream<EventWrapperBase>(streamId);
        var eventWrapper = new EventWrapper<T>(@event, Guid.NewGuid(), this.GetGrainId());
        await stream.OnNextAsync(eventWrapper);
    }

    #endregion
}

public interface IWorkflowCoordinatorGAgent : IStateGAgent<WorkflowCoordinatorState>
{
}