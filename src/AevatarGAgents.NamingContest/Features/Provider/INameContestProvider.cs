using System;
using System.Threading.Tasks;
using AevatarGAgents.NamingContest.Common;
using AevatarGAgents.NamingContest.VoteGAgent;

namespace AevatarGAgents.NamingContest.Provider;

public interface INameContestProvider
{
    public Task SendMessageAsync(Guid groupId,NamingAILogEvent? namingLogEvent,string callBackUrl);
    public Task SendMessageAsync(Guid groupId,VoteCharmingCompleteEvent? namingLogEvent,string callBackUrl);
}