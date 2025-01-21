
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.Basic.GroupGAgent;
using Aevatar.GAgents.Basic.PublishGAgent;
using AiSmart.GAgent.NamingContest.VoteAgent.Dto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AiSmart.GAgent.NamingContest.VoteAgent;

[GAgent(nameof(VoteCharmingGAgent))]
public class VoteCharmingGAgent : GAgentBase<VoteCharmingState, VoteCharmingStateLogEvent, EventBase, InitVoteAgent>, IVoteCharmingGAgent
{
    public VoteCharmingGAgent(ILogger<VoteCharmingGAgent> logger) : base(logger)
    {
    }

    [EventHandler]
    public async Task HandleEventAsync(VoteCharmingGEvent @event)
    {
        Logger.LogInformation(
            "VoteCharmingEvent receive {info},TotalBatches:{TotalBatches},CurrentBatch:{CurrentBatch}",
            JsonConvert.SerializeObject(@event), State.TotalBatches, State.CurrentBatch);

        if (State.GroupList.Count == 0)
        {
            return;
        }

        var voteGroupList = GetVoteGroupList();
        if (voteGroupList.Count == 0)
        {
            Logger.LogInformation("[VoteCharmingGAgent] VoteCharmingEvent trafficList.Count == 0 ");
        }

        foreach (var groupId in voteGroupList)
        {
            var groupAgent = GrainFactory.GetGrain<IGroupGAgent>(groupId);
            // var childrenAgent = await groupAgent.GetChildrenAsync();
            // var publishAgentId = childrenAgent.FirstOrDefault(f => f.ToString().StartsWith("publishinggagent"));
            // IPublishingGAgent publishAgent;
            // if (!publishAgentId.IsDefault)
            // {
            //     publishAgent = GrainFactory.GetGrain<IPublishingGAgent>(publishAgentId);
            // }
            // else
            // {
            //     publishAgent = GrainFactory.GetGrain<IPublishingGAgent>(new Guid());
            //     await groupAgent.RegisterAsync(publishAgent);
            // }

            await groupAgent.PublishEventAsync(new SingleVoteCharmingGEvent
            {
                AgentIdNameDictionary = @event.AgentIdNameDictionary,
                VoteMessage = @event.VoteMessage,
                Round = @event.Round,
                VoteCharmingGrainId = this.GetPrimaryKey()
            });

            Logger.LogInformation("SingleVoteCharmingEvent send");
        }

        RaiseEvent(new GroupVoteCompleteStateLogEvent
        {
            VoteGroupList = voteGroupList,
        });

        await ConfirmEvents();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Represents an agent responsible for voting charming agents.");
    }

    private List<GrainId> GetVoteGroupList()
    {
        if (State.TotalGroupCount <= State.GroupHasVoteCount + 1)
        {
            return State.GroupList;
        }

        var result = new List<GrainId>();
        var random = new Random();
        var basicDenominator = Math.Ceiling((double)State.TotalGroupCount / 2);
        var basicNumerator = Math.Abs(basicDenominator - State.GroupList.Count);
        if (basicNumerator / 2 >= random.Next(0, (int)basicNumerator))
        {
            return result;
        }

        var basis = (double)basicNumerator / (double)basicDenominator;
        int randomCount = (int)Math.Ceiling(State.GroupList.Count * (1 - basis));
        randomCount = Math.Max(randomCount / 2, randomCount);
        if (randomCount == 0)
        {
            return result;
        }

        result = State.GroupList.OrderBy(x => random.Next()).Take(randomCount).ToList();
        return result;
    }

    public override async Task InitializeAsync(InitVoteAgent initializeDto)
    { 
        RaiseEvent(new InitVoteCharmingStateLogEvent
        {
            GrainGuidList = new List<Guid>(),
            TotalBatches = initializeDto.TotalBatches,
            Round = initializeDto.Round,
            GrainGuidTypeDictionary = new Dictionary<Guid, string>(),
            GroupList = initializeDto.GroupList,
        });

        await ConfirmEvents();
    }
}