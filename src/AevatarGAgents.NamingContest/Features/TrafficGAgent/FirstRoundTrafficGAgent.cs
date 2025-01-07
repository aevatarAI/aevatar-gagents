using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core;
using AevatarGAgents.MicroAI.Agent;
using AevatarGAgents.MicroAI.Agent.GEvents;
using AevatarGAgents.MicroAI.Grains;
using AevatarGAgents.NamingContest.Common;
using AutoGen.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Aevatar.Core.Abstractions;
using AevatarGAgents.NamingContest.JudgeGAgent;
using AevatarGAgents.NamingContest.VoteGAgent;

namespace AevatarGAgents.NamingContest.TrafficGAgent;

public class FirstRoundTrafficGAgent : GAgentBase<FirstTrafficState, TrafficEventSourcingBase>, IFirstTrafficGAgent
{
    public FirstRoundTrafficGAgent(ILogger<FirstRoundTrafficGAgent> logger) : base(logger)
    {
    }

    [EventHandler]
    public async Task HandleEventAsync(GroupStartEvent @event)
    {
        RaiseEvent(new TrafficNameStartSEvent { Content = @event.Message });
        RaiseEvent(new ChangeNamingStepSEvent { Step = NamingContestStepEnum.NamingStart });
        RaiseEvent(new AddChatHistorySEvent()
            { ChatMessage = new MicroAIMessage(Role.User.ToString(), @event.Message) });
        await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.NamingStart, Guid.Empty));
        await PublishAsync(new GroupChatStartGEvent() { IfFirstStep = true, ThemeDescribe = @event.Message });
        await DispatchCreativeAgent();

        RaiseEvent(new ChangeNamingStepSEvent { Step = NamingContestStepEnum.Naming });

        await base.ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(NamedCompleteGEvent @event)
    {
        if (State.CurrentGrainId != @event.GrainGuid)
        {
            Logger.LogError(
                $"Traffic NamedCompleteGEvent Current GrainId not match {State.CurrentGrainId.ToString()}--{@event.GrainGuid.ToString()}");
            return;
        }

        base.RaiseEvent(new TrafficGrainCompleteSEvent()
        {
            CompleteGrainId = @event.GrainGuid,
        });

        base.RaiseEvent(new AddChatHistorySEvent()
        {
            ChatMessage = new MicroAIMessage(Role.User.ToString(),
                AssembleMessageUtil.AssembleNamingContent(@event.CreativeName, @event.NamingReply))
        });

        base.RaiseEvent(new CreativeNamingSEvent() { CreativeId = @event.GrainGuid, Naming = @event.NamingReply });

        await base.ConfirmEvents();

        await DispatchCreativeAgent();
    }

    [EventHandler]
    public async Task HandleEventAsync(DebatedCompleteGEvent @event)
    {
        if (State.CurrentGrainId != @event.GrainGuid)
        {
            Logger.LogError(
                $"Traffic DebatedCompleteGEvent Current GrainId not match {State.CurrentGrainId.ToString()}--{@event.GrainGuid.ToString()}");
            return;
        }

        base.RaiseEvent(new AddChatHistorySEvent()
        {
            ChatMessage = new MicroAIMessage(Role.User.ToString(),
                AssembleMessageUtil.AssembleDebateContent(@event.CreativeName, @event.DebateReply))
        });

        base.RaiseEvent(new TrafficGrainCompleteSEvent()
        {
            CompleteGrainId = @event.GrainGuid,
        });

        await base.ConfirmEvents();

        await DispatchDebateAgent();
    }

    [EventHandler]
    public async Task HandleEventAsync(JudgeVoteResultGEvent @event)
    {
        if (State.CurrentGrainId != @event.JudgeGrainId)
        {
            Logger.LogError(
                $"Traffic HandleEventAsync Current GrainId not match {State.CurrentGrainId.ToString()}--{@event.JudgeGrainId.ToString()}");
            return;
        }

        var creativeInfo = State.CreativeList.FirstOrDefault(f => f.Naming == @event.VoteName);
        if (creativeInfo != null)
        {
            var voteInfoStr = JsonConvert.SerializeObject(new JudgeVoteInfo()
            {
                AgentId = creativeInfo.CreativeGrainId, AgentName = creativeInfo.CreativeName,
                Nameing = @event.VoteName, Reason = @event.Reason
            });

            await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.JudgeVote, @event.JudgeGrainId,
                NamingRoleType.Judge, @event.JudgeName, voteInfoStr));
        }

        base.RaiseEvent(new TrafficGrainCompleteSEvent()
        {
            CompleteGrainId = @event.JudgeGrainId,
        });

        await base.ConfirmEvents();

        await DispatchJudgeAgent();
    }

    [EventHandler]
    public async Task HandleEventAsync(HostSummaryCompleteGEvent @event)
    {
        if (State.CurrentGrainId != @event.HostId)
        {
            Logger.LogError(
                $"Traffic HandleEventAsync Current GrainId not match {State.CurrentGrainId.ToString()}--{@event.HostId.ToString()}");
            return;
        }

        base.RaiseEvent(new TrafficGrainCompleteSEvent()
        {
            CompleteGrainId = @event.HostId,
        });

        await base.ConfirmEvents();

        await DispatchHostAgent();
    }

    public Task<MicroAIGAgentState> GetStateAsync()
    {
        throw new NotImplementedException();
    }

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    private async Task DispatchCreativeAgent()
    {
        var random = new Random();
        var creativeList = State.CreativeList.FindAll(f => State.CalledGrainIdList.Contains(f.CreativeGrainId) == false)
            .ToList();
        if (creativeList.Count == 0)
        {
            await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.DebateStart, Guid.Empty));

            // end message 
            await PublishAsync(new TrafficNamingContestOver() { NamingQuestion = State.NamingContent });

            RaiseEvent(new ClearCalledGrainsSEvent());

            var debateRound = random.Next(1, 3);
            RaiseEvent(new SetDebateCountSEvent() { DebateCount = debateRound });
            RaiseEvent(new ChangeNamingStepSEvent { Step = NamingContestStepEnum.Debate });

            await base.ConfirmEvents();

            // begin the second stage debate
            _ = DispatchDebateAgent();

            return;
        }

        // random select one Agent
        var index = random.Next(0, creativeList.Count);
        var selectedInfo = creativeList[index];
        RaiseEvent(new TrafficCallSelectGrainIdSEvent() { GrainId = selectedInfo.CreativeGrainId });
        await base.ConfirmEvents();

        // route to the selectedId Agent 
        await PublishAsync(new TrafficInformCreativeGEvent()
            { CreativeGrainId = selectedInfo.CreativeGrainId });
    }

    private async Task DispatchDebateAgent()
    {
        // the second stage - debate
        var creativeList = State.CreativeList.FindAll(f => State.CalledGrainIdList.Contains(f.CreativeGrainId) == false)
            .ToList();
        if (State.DebateRoundCount == 0 && creativeList.Count == 0)
        {
            await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.JudgeVoteStart, Guid.Empty));
            RaiseEvent(new ClearCalledGrainsSEvent());
            await base.ConfirmEvents();

            // begin the third stage judge
            _ = DispatchJudgeAgent();
            return;
        }

        if (creativeList.Count == 0 && State.DebateRoundCount > 0)
        {
            creativeList = State.CreativeList;
            RaiseEvent(new ReduceDebateRoundSEvent());
            RaiseEvent(new ClearCalledGrainsSEvent());
        }

        // random select one Agent
        var random = new Random();
        var index = random.Next(0, creativeList.Count);
        var selectedInfo = creativeList[index];
        RaiseEvent(new TrafficCallSelectGrainIdSEvent() { GrainId = selectedInfo.CreativeGrainId });
        await base.ConfirmEvents();

        // route to the selectedId Agent 
        await PublishAsync(new TrafficInformDebateGEvent()
            { CreativeGrainId = selectedInfo.CreativeGrainId });
    }

    private async Task DispatchJudgeAgent()
    {
        var creativeList = State.JudgeAgentList.FindAll(f => State.CalledGrainIdList.Contains(f) == false).ToList();
        if (creativeList.Count == 0)
        {
            await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.Complete, Guid.Empty));
            await PublishAsync(new NamingContestComplete());

            await PublishMostCharmingEventAsync();
            await DispatchHostAgent();
            return;
        }

        var random = new Random();
        var index = random.Next(0, creativeList.Count);
        var selectedId = creativeList[index];
        RaiseEvent(new TrafficCallSelectGrainIdSEvent() { GrainId = selectedId });
        await base.ConfirmEvents();

        await PublishAsync(new JudgeVoteGEVent() { JudgeGrainId = selectedId, History = State.ChatHistory });
    }

    private async Task PublishMostCharmingEventAsync()
    {
        await PublishAsync(new VoteCharmingEvent()
        {
            AgentIdNameDictionary = State.CreativeList.ToDictionary(p => p.CreativeGrainId, p => p.CreativeName),
            Round = 1,
            VoteMessage = State.ChatHistory
        });
    }

    private async Task DispatchHostAgent()
    {
        var hostAgentList = State.HostAgentList.FindAll(f => State.CalledGrainIdList.Contains(f) == false).ToList();
        if (hostAgentList.Count == 0)
        {
            await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.HostSummaryComplete, Guid.Empty));
            return;
        }

        var random = new Random();
        var index = random.Next(0, hostAgentList.Count);
        var selectedId = hostAgentList[index];
        RaiseEvent(new TrafficCallSelectGrainIdSEvent() { GrainId = selectedId });
        await base.ConfirmEvents();

        await PublishAsync(new HostSummaryGEvent() { HostId = selectedId, History = State.ChatHistory });
    }

    public async Task SetAgent(string agentName, string agentResponsibility)
    {
        RaiseEvent(new TrafficSetAgentSEvent
        {
            AgentName = agentName,
            Description = agentResponsibility
        });
        await ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(agentName).SetAgentAsync(agentResponsibility);
    }

    public async Task SetAgentWithTemperatureAsync(string agentName, string agentResponsibility, float temperature,
        int? seed = null,
        int? maxTokens = null)
    {
        RaiseEvent(new TrafficSetAgentSEvent
        {
            AgentName = agentName,
            Description = agentResponsibility
        });
        await ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(agentName)
            .SetAgentWithTemperature(agentResponsibility, temperature, seed, maxTokens);
    }

    public Task<MicroAIGAgentState> GetAgentState()
    {
        throw new NotImplementedException();
    }

    public async Task AddCreativeAgent(string creativeName, Guid creativeGrainId)
    {
        RaiseEvent(new AddCreativeAgent() { CreativeGrainId = creativeGrainId, CreativeName = creativeName });
        await base.ConfirmEvents();
    }

    public async Task AddJudgeAgent(Guid judgeGrainId)
    {
        RaiseEvent(new AddJudgeSEvent() { JudgeGrainId = judgeGrainId });
        await ConfirmEvents();
    }

    public async Task AddHostAgent(Guid judgeGrainId)
    {
        RaiseEvent(new AddHostSEvent() { HostGrainId = judgeGrainId });
        await ConfirmEvents();
    }
}