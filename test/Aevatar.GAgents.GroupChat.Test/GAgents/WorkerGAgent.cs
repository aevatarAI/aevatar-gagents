using Aevatar.Core.Abstractions;
using GroupChat.GAgent;
using GroupChat.GAgent.Dto;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Orleans;

namespace Aevatar.GAgents.GroupChat.Test.GAgents;

[GAgent(nameof(WorkerGAgentGAgent))]
public class WorkerGAgentGAgent : GroupMemberGAgentBase<WorkerState, WorkerEventLog, EventBase, GroupMemberConfigDto>, IWorkerGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Group chat Worker GAgent");
    }

    public Task<WorkerState> GetState()
    {
        return Task.FromResult(State);
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        var random = new Random();
        
        return Task.FromResult(random.Next(1, 90));
    }

    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        var response = new ChatResponse();
        response.Content = $"{State.MemberName} Send the message";
        RaiseEvent(new WorkHandleMessageLogEvent(){ PreWorkUnits = coordinatorMessages!.Select(s=>s.AgentName).ToList()});
        await ConfirmEvents();

        Console.WriteLine($"{State.MemberName} Can Speak, receive:{coordinatorMessages!.Select(s => s.Content).ToList().JoinAsString(" ")}");
        return response;
    }

    protected override void GroupMemberTransitionState(WorkerState state, StateLogEventBase<WorkerEventLog> @event)
    {
        switch (@event)
        {
            case WorkHandleMessageLogEvent workHandleMessageLogEvent:
                State.PreWorkUnits = workHandleMessageLogEvent.PreWorkUnits;
                return;
        }
    }
}

[GenerateSerializer]
public class WorkerEventLog : StateLogEventBase<WorkerEventLog>
{
}


[GenerateSerializer]
public class  WorkHandleMessageLogEvent : WorkerEventLog
{
    [Id(0)] public List<string> PreWorkUnits { get; set; } = new List<string>();
}


public interface IWorkerGAgent : IStateGAgent<WorkerState>
{
}

[GenerateSerializer]
public class WorkerState : GroupMemberState
{
    [Id(0)] public List<string> PreWorkUnits = new List<string>();
}