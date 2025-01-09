using System;
using System.Threading.Tasks;
using Aevatar.GAgents.NamingContest.Common;
using Aevatar.GAgents.NamingContest.VoteGAgent;
using Orleans;

namespace Aevatar.GAgents.NamingContest.Grains;

public interface INamingContestGrain : IGrainWithStringKey
{
    public Task SendMessageAsync(Guid groupId,NamingAILogEvent? nameContentGEvent,string callBackUrl);
    public Task SendMessageAsync(Guid groupId,VoteCharmingCompleteEvent? nameContentGEvent,string callBackUrl);
   
}