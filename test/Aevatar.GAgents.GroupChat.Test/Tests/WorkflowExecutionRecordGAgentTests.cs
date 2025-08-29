using Aevatar.Core.Abstractions;
using Aevatar.Core;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Core.Dto;
using Aevatar.GAgents.GroupChat.Core.States;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Newtonsoft.Json;
using Shouldly;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

public class WorkflowExecutionRecordGAgentTests : AevatarGroupChatTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public WorkflowExecutionRecordGAgentTests()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task Handle_StartExecuteWorkflowEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        var startExecuteWorkflowEvent = new StartExecuteWorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            RoundId = 100,
            Content = "Init",
            WorkUnitInfos = new List<WorkUnitInfo>
            {
                new WorkUnitInfo
                {
                    GrainId = workerGrainId.ToString(), NextGrainId = "", UnitStatusEnum = WorkerUnitStatusEnum.Pending
                }
            }
        };

        await groupAgent.PublishEventAsync(startExecuteWorkflowEvent);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        state.WorkflowId.ShouldBe(startExecuteWorkflowEvent.WorkflowId);
        state.RoundId.ShouldBe(startExecuteWorkflowEvent.RoundId);
        state.InitContent.ShouldBe(startExecuteWorkflowEvent.Content);
        state.Status.ShouldBe(WorkflowExecutionStatus.Running);
        state.WorkUnitInfos.Count.ShouldBe(1);
        state.WorkUnitInfos.ShouldContain(o => o.GrainId == workerGrainId.ToString());
        state.WorkUnitRecords.Count.ShouldBe(1);
        state.WorkUnitRecords.ShouldContain(o => o.WorkUnitGrainId == workerGrainId.ToString());
    }

    [Fact]
    public async Task Handle_GroupChatFinishEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var finishExecuteWorkflowEvent = new GroupChatFinishEvent
        {
        };

        await groupAgent.PublishEventAsync(finishExecuteWorkflowEvent);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        state.Status.ShouldBe(WorkflowExecutionStatus.Completed);
    }

    [Fact]
    public async Task Handle_StartExecuteWorkUnitEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Running);
        grainARecord.InputData.ShouldBe(JsonConvert.SerializeObject(startExecuteGrain.CoordinatorMessages));
    }

    [Fact]
    public async Task Handle_ChatResponseEvent_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Running);
        grainARecord.InputData.ShouldBe(JsonConvert.SerializeObject(startExecuteGrain.CoordinatorMessages));

        var finishExecuteGrainA = new ChatResponseEvent
        {
            PublisherGrainId = workerGrainId,
            ChatResponse = new ChatResponse
            {
                Content = "Grain response"
            }
        };
        await groupAgent.PublishEventAsync(finishExecuteGrainA);
        await Task.Delay(1000);

        state = await recordAgent.GetStateAsync();
        grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.OutputData.ShouldBe(JsonConvert.SerializeObject(finishExecuteGrainA.ChatResponse.Content));
    }

    [Fact]
    public async Task Handle_ChatResponseEvent_Failure_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(500);

        var failure = new ChatResponseEvent
        {
            PublisherGrainId = workerGrainId,
            FailureSummary = "unit crashed"
        };
        await groupAgent.PublishEventAsync(failure);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        state.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        var unit = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        unit.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        unit.FailureSummary.ShouldBe("unit crashed");
        unit.EndTime.ShouldNotBeNull();
    }

    [Fact]
    public async Task IncorrectSequence_Test()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);
        var workerGrainId = groupAgent.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var finishExecuteGrainA = new ChatResponseEvent
        {
            PublisherGrainId = workerGrainId,
            ChatResponse = new ChatResponse
            {
                Content = "Grain response"
            }
        };
        await groupAgent.PublishEventAsync(finishExecuteGrainA);
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        var grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.OutputData.ShouldBe(JsonConvert.SerializeObject(finishExecuteGrainA.ChatResponse.Content));

        var startExecuteGrain = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input A" } }
        };
        await groupAgent.PublishEventAsync(startExecuteGrain);
        await Task.Delay(1000);

        state = await recordAgent.GetStateAsync();
        grainARecord = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        grainARecord.Status.ShouldBe(WorkflowExecutionStatus.Completed);
        grainARecord.InputData.ShouldBe(JsonConvert.SerializeObject(startExecuteGrain.CoordinatorMessages));
    }

    [Fact]
    public async Task ChatEvent_SpeakerMismatch_ShouldNotChangeRecord()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);

        var worker = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(worker);
        var workerGrainId = worker.GetGrainId();

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteWorkUnit = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input" } }
        };
        await groupAgent.PublishEventAsync(startExecuteWorkUnit);
        await Task.Delay(500);

        // Publish ChatEvent with mismatched speaker; member should ignore and not emit ChatResponseEvent
        await groupAgent.PublishEventAsync(new ChatEvent
        {
            BlackboardId = Guid.NewGuid(),
            Speaker = Guid.NewGuid(),
            Term = 0
        });
        await Task.Delay(800);

        var state = await recordAgent.GetStateAsync();
        var unit = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        unit.Status.ShouldBe(WorkflowExecutionStatus.Running);
        unit.OutputData.ShouldBeNull();
    }

    [Fact]
    public async Task ChatEvent_Exception_ShouldFailRecord()
    {
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var recordAgent = await _agentFactory.GetGAgentAsync<IWorkflowExecutionRecordGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(recordAgent);

        var worker = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await groupAgent.RegisterAsync(worker);
        var workerGrainId = worker.GetGrainId();

        // Configure worker to throw from ChatAsync
        await worker.SetFailureSummary("boom");

        await StartExecuteWorkflowAsync(groupAgent, workerGrainId);

        var startExecuteWorkUnit = new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = workerGrainId.ToString(),
            CoordinatorMessages = new List<ChatMessage> { new ChatMessage { Content = "Input" } }
        };
        await groupAgent.PublishEventAsync(startExecuteWorkUnit);
        await Task.Delay(500);

        // Trigger ChatEvent with correct speaker so the member processes and throws
        await groupAgent.PublishEventAsync(new ChatEvent
        {
            BlackboardId = Guid.NewGuid(),
            Speaker = workerGrainId.GetGuidKey(),
            Term = 0
        });
        await Task.Delay(1000);

        var state = await recordAgent.GetStateAsync();
        state.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        var unit = state.WorkUnitRecords.First(o => o.WorkUnitGrainId == workerGrainId.ToString());
        unit.Status.ShouldBe(WorkflowExecutionStatus.Failed);
        unit.FailureSummary.ShouldContain("boom");
        unit.EndTime.ShouldNotBeNull();
    }

    private async Task StartExecuteWorkflowAsync(IGroupGAgent groupAgent, GrainId workerGrainId)
    {
        var startExecuteWorkflowEvent = new StartExecuteWorkflowEvent
        {
            WorkflowId = Guid.NewGuid(),
            RoundId = 100,
            Content = "Init",
            WorkUnitInfos = new List<WorkUnitInfo>
            {
                new WorkUnitInfo
                {
                    GrainId = workerGrainId.ToString(), NextGrainId = "", UnitStatusEnum = WorkerUnitStatusEnum.Pending
                }
            }
        };

        await groupAgent.PublishEventAsync(startExecuteWorkflowEvent);
        await Task.Delay(1000);
    }

    [Fact]
    public async Task Member_GetDescription_ShouldIncludeMemberName()
    {
        var member = await _agentFactory.GetGAgentAsync<ITestMemberHelperGAgent>(Guid.NewGuid());
        await member.ConfigAsync(new GroupMemberConfigDto { MemberName = "Alice" });

        var desc = await member.DescribeAsync();
        desc.ShouldContain("Member Name: Alice");
    }

    [Fact]
    public async Task Member_Ping_ShouldPublishPong()
    {
        var group = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var member = await _agentFactory.GetGAgentAsync<ITestMemberHelperGAgent>(Guid.NewGuid());
        await member.ConfigAsync(new GroupMemberConfigDto { MemberName = "Pingy" });
        var collector = await _agentFactory.GetGAgentAsync<IEventCollectorGAgent>(Guid.NewGuid());

        await group.RegisterAsync(member);
        await group.RegisterAsync(collector);

        var blackboardId = Guid.NewGuid();
        await group.PublishEventAsync(new CoordinatorPingEvent { BlackboardId = blackboardId });
        await Task.Delay(500);

        var s = await collector.GetStateAsync();
        s.LastPongBlackboardId.ShouldBe(blackboardId);
        s.LastPongMemberName.ShouldBe("Pingy");
        s.LastPongMemberId.ShouldBe(member.GetGrainId().GetGuidKey());
    }

    [Fact]
    public async Task Member_EvaluationInterest_ShouldPublishResponse()
    {
        var group = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var member = await _agentFactory.GetGAgentAsync<ITestMemberHelperGAgent>(Guid.NewGuid());
        await member.ConfigAsync(new GroupMemberConfigDto { MemberName = "Eva" });
        var collector = await _agentFactory.GetGAgentAsync<IEventCollectorGAgent>(Guid.NewGuid());

        await group.RegisterAsync(member);
        await group.RegisterAsync(collector);

        var blackboardId = Guid.NewGuid();
        await group.PublishEventAsync(new EvaluationInterestEvent { BlackboardId = blackboardId, ChatTerm = 123 });
        await Task.Delay(500);

        var s = await collector.GetStateAsync();
        s.LastInterestBlackboardId.ShouldBe(blackboardId);
        s.LastInterestMemberId.ShouldBe(member.GetGrainId().GetGuidKey());
        s.LastInterestValue.ShouldBe(77);
        s.LastInterestChatTerm.ShouldBe(123);
    }

    [Fact]
    public async Task Member_GetMessageFromBlackboard_ShouldReturnContent()
    {
        var member = await _agentFactory.GetGAgentAsync<ITestMemberHelperGAgent>(Guid.NewGuid());
        await member.ConfigAsync(new GroupMemberConfigDto { MemberName = "Reader" });

        var blackboardId = Guid.NewGuid();
        var blackboard = await _agentFactory.GetGAgentAsync<IBlackboardGAgent>(blackboardId);
        await blackboard.SetTopic("topic-x");

        var msgs = await member.FetchBlackboardMessages(blackboardId);
        msgs.ShouldNotBeNull();
        msgs.Any(m => m.MessageType == MessageType.BlackboardTopic && m.Content == "topic-x").ShouldBeTrue();
    }

    // Test helper agents for coverage
    [GAgent(nameof(EventCollectorGAgent))]
    public class EventCollectorGAgent : GroupMemberGAgentBase<CollectorState, CollectorLogEvent, EventBase, GroupMemberConfigDto>, IEventCollectorGAgent
    {
        public override Task<string> GetDescriptionAsync() => Task.FromResult("collector");

        protected override Task<int> GetInterestValueAsync(Guid blackboardId) => Task.FromResult(0);

        protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
        {
            return Task.FromResult(new ChatResponse { Content = "noop" });
        }

        [EventHandler]
        public async Task HandleEventAsync(CoordinatorPongEvent @event)
        {
            RaiseEvent(new SetPongLogEvent
            {
                BlackboardId = @event.BlackboardId,
                MemberId = @event.MemberId,
                MemberName = @event.MemberName
            });
            await ConfirmEvents();
        }

        [EventHandler]
        public async Task HandleEventAsync(EvaluationInterestResponseEvent @event)
        {
            RaiseEvent(new SetInterestLogEvent
            {
                BlackboardId = @event.BlackboardId,
                MemberId = @event.MemberId,
                InterestValue = @event.InterestValue,
                ChatTerm = @event.ChatTerm
            });
            await ConfirmEvents();
        }

        protected override void GroupMemberTransitionState(CollectorState state, StateLogEventBase<CollectorLogEvent> @event)
        {
            switch (@event)
            {
                case SetPongLogEvent e:
                    state.LastPongBlackboardId = e.BlackboardId;
                    state.LastPongMemberId = e.MemberId;
                    state.LastPongMemberName = e.MemberName;
                    return;
                case SetInterestLogEvent e2:
                    state.LastInterestBlackboardId = e2.BlackboardId;
                    state.LastInterestMemberId = e2.MemberId;
                    state.LastInterestValue = e2.InterestValue;
                    state.LastInterestChatTerm = e2.ChatTerm;
                    return;
            }
        }
    }

    public interface IEventCollectorGAgent : IStateGAgent<CollectorState>
    {
    }

    [GenerateSerializer]
    public class CollectorLogEvent : StateLogEventBase<CollectorLogEvent>
    {
    }

    [GenerateSerializer]
    public class SetPongLogEvent : CollectorLogEvent
    {
        [Id(0)] public Guid BlackboardId { get; set; }
        [Id(1)] public Guid MemberId { get; set; }
        [Id(2)] public string MemberName { get; set; }
    }

    [GenerateSerializer]
    public class SetInterestLogEvent : CollectorLogEvent
    {
        [Id(0)] public Guid BlackboardId { get; set; }
        [Id(1)] public Guid MemberId { get; set; }
        [Id(2)] public int InterestValue { get; set; }
        [Id(3)] public long ChatTerm { get; set; }
    }

    [GenerateSerializer]
    public class CollectorState : WorkerState
    {
        [Id(10)] public Guid LastPongBlackboardId { get; set; }
        [Id(11)] public Guid LastPongMemberId { get; set; }
        [Id(12)] public string LastPongMemberName { get; set; }
        [Id(13)] public Guid LastInterestBlackboardId { get; set; }
        [Id(14)] public Guid LastInterestMemberId { get; set; }
        [Id(15)] public int LastInterestValue { get; set; }
        [Id(16)] public long LastInterestChatTerm { get; set; }
    }

    [GAgent(nameof(TestMemberHelperGAgent))]
    public class TestMemberHelperGAgent : GroupMemberGAgentBase<WorkerState, TestMemberEventLog, EventBase, GroupMemberConfigDto>, ITestMemberHelperGAgent
    {
        protected override Task<int> GetInterestValueAsync(Guid blackboardId) => Task.FromResult(77);

        protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
        {
            return Task.FromResult(new ChatResponse { Content = "ok" });
        }

        public Task<List<ChatMessage>> FetchBlackboardMessages(Guid blackboardId) => GetMessageFromBlackboardAsync(blackboardId);

        public Task<string> DescribeAsync() => GetDescriptionAsync();
    }

    public interface ITestMemberHelperGAgent : IStateGAgent<WorkerState>
    {
        Task<List<ChatMessage>> FetchBlackboardMessages(Guid blackboardId);
        Task<string> DescribeAsync();
    }

    [GenerateSerializer]
    public class TestMemberEventLog : StateLogEventBase<TestMemberEventLog>
    {
    }
}