using System;
using System.Threading.Tasks;
using AevatarGAgents.NamingContest.Common;
using AevatarGAgents.NamingContest.VoteGAgent;
using Orleans;

namespace AevatarGAgents.NamingContest.Grains;

public interface INamingContestGrain : IGrainWithStringKey
{
    public Task SendMessageAsync(Guid groupId,NamingAILogEvent? nameContentGEvent,string callBackUrl);
    public Task SendMessageAsync(Guid groupId,VoteCharmingCompleteEvent? nameContentGEvent,string callBackUrl);
   
}