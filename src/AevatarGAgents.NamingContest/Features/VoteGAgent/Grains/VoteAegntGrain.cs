using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using AevatarGAgents.Autogen.Common;
using AevatarGAgents.MicroAI.Provider;
using AevatarGAgents.NamingContest.Common;
using AutoGen.Core;
using AutoGen.SemanticKernel;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;

namespace AevatarGAgents.NamingContest.VoteGAgent.Grains;

public class VoteAegntGrain : Grain,IVoteAgentGrain
{
    private IStreamProvider StreamProvider => this.GetStreamProvider(CommonConstants.StreamProvider);
    private MiddlewareStreamingAgent<SemanticKernelAgent>? _agent;
    private IAIAgentProvider _aiAgentProvider;

    public VoteAegntGrain(IAIAgentProvider aiAgentProvider
        )
    {
        _aiAgentProvider = aiAgentProvider;
    }

    public async Task VoteAgentAsync(VoteCharmingEvent voteCharmingEvent)
    {
      var microAIMessage  = await _aiAgentProvider.SendAsync(_agent, JsonConvert.ToString(voteCharmingEvent)+"  The above JSON contains each GUID with their names and associated conversations. Please select the GUID that is most appealing to you", null);
      if (microAIMessage.Content != null)
      {
          await PublishEventAsync(new VoteCharmingCompleteEvent
          {
              Winner = Guid.Parse(microAIMessage.Content),
              VoterId = this.GetPrimaryKey(),
              Round = 1
          });
      }
    }

    public async Task VoteAgentAsync(SingleVoteCharmingEvent singleVoteCharmingEvent)
    {
        var agentNames = string.Join(" and ", singleVoteCharmingEvent.AgentIdNameDictionary.Values);
        var message  = await _aiAgentProvider.SendAsync(_agent, NamingConstants.VotePrompt.Replace("$AgentNames$",agentNames),singleVoteCharmingEvent.VoteMessage);
        if (message.Content != null)
        {
            var namingReply = message.Content.Replace("\"","").ToLower();
            var agent = singleVoteCharmingEvent.AgentIdNameDictionary.FirstOrDefault(x => x.Value.ToLower().Equals(namingReply));
            var winner = agent.Key;

            await PublishEventAsync(new VoteCharmingCompleteEvent
            {
                Winner = winner,
                VoterId = this.GetPrimaryKey(),
                Round = 1
            });
        }
    }

    private async Task PublishEventAsync(EventBase publishData)
    {
        var streamId = StreamId.Create(CommonConstants.StreamNamespace, this.GetGrainId().ToString());
        var stream = StreamProvider.GetStream<EventBase>(streamId);
        await stream.OnNextAsync(publishData);
    }

    public override async Task<Task> OnActivateAsync(CancellationToken cancellationToken)
    {
        _agent = await _aiAgentProvider.GetAgentAsync("", "");
        return base.OnActivateAsync(cancellationToken);
    }
}