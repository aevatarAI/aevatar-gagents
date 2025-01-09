using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.Agent;
using Aevatar.GAgents.NamingContest.Common;
using Aevatar.GAgents.NamingContest.CreativeGAgent;
using Aevatar.GAgents.NamingContest.JudgeGAgent;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.NamingContest.VoteGAgent;

public class VoteCharmingGAgent : GAgentBase<VoteCharmingState, GEventBase>, IVoteCharmingGAgent
{

    public VoteCharmingGAgent( ILogger<VoteCharmingGAgent> logger) : base(logger)
    {
    }
    
    [EventHandler]
    public async Task HandleEventAsync(InitVoteCharmingEvent @event)
    {
        if (!State.VoterIds.IsNullOrEmpty())
        {
            return;
        }

        var random = new Random();
        var list = new List<Guid>();
        list.AddRange(@event.CreativeGuidList);
        list.AddRange(@event.JudgeGuidList);

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        var grainGuidTypeDictionary = new Dictionary<Guid, string>();
        foreach (var agentId in @event.CreativeGuidList)
        {
            grainGuidTypeDictionary.TryAdd(agentId, NamingConstants.AgentPrefixCreative);
        }
        foreach (var agentId in @event.JudgeGuidList)
        {
            grainGuidTypeDictionary.TryAdd(agentId, NamingConstants.AgentPrefixJudge);
        }

        base.RaiseEvent(new InitVoteCharmingGEvent
        {
            GrainGuidList = list,
            TotalBatches =  @event.TotalBatches,
            Round = @event.Round,
            GrainGuidTypeDictionary = grainGuidTypeDictionary
        });
        await ConfirmEvents();
    }
    
    [EventHandler]
    public async Task HandleEventAsync(VoteCharmingEvent @event)
    {
        if (State.TotalBatches == State.CurrentBatch)
        {
            return;
        }

        var actualBatchSize = 0;
        if (State.TotalBatches == State.CurrentBatch + 1)
        {
            actualBatchSize = State.VoterIds.Count;
        }
        else
        {
            var averageBatchSize = State.VoterIds.Count / State.TotalBatches;
            var minBatchSize = Math.Max(1, (int)(averageBatchSize * 0.6)); 
            var maxBatchSize = Math.Min(State.VoterIds.Count, (int)(averageBatchSize * 1.5)); 
            var random = new Random();
            actualBatchSize = random.Next(minBatchSize, maxBatchSize);
        }

        var selectedVoteIds = State.VoterIds.GetRange(0, actualBatchSize);
        foreach (var voteId in selectedVoteIds)
        {
            if (!State.VoterIdTypeDictionary.TryGetValue(voteId, out var grainClassNamePrefix))
            {
                continue;
            }

            switch (grainClassNamePrefix)
            {
                case NamingConstants.AgentPrefixCreative:
                    await  RegisterAsync(GrainFactory.GetGrain<ICreativeGAgent>(voteId));
                    break;
                case NamingConstants.AgentPrefixJudge:
                    await  RegisterAsync(GrainFactory.GetGrain<IJudgeGAgent>(voteId));
                    break;
            }
        }

        await PublishAsync(new SingleVoteCharmingEvent
        {
            AgentIdNameDictionary = @event.AgentIdNameDictionary,
            VoteMessage = @event.VoteMessage,
            Round = @event.Round
        });
        base.RaiseEvent(new VoteCharmingGEvent
        {
            GrainGuidList = selectedVoteIds
        });

        await ConfirmEvents();
    }
    
    
    
    [EventHandler]
    public async Task HandleEventAsync(VoteCharmingCompleteEvent @event)
    {
        await  UnregisterAsync(GrainFactory.GetGrain<IMicroAIGAgent>(@event.VoterId));
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for voting charming agents.");
    }
}