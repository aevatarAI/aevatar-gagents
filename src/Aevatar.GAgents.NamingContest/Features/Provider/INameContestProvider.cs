using System;
using System.Threading.Tasks;
using Aevatar.GAgents.NamingContest.Common;
using Aevatar.GAgents.NamingContest.VoteGAgent;

namespace Aevatar.GAgents.NamingContest.Provider;

public interface INameContestProvider
{
    public Task SendMessageAsync(Guid groupId,NamingAILogEvent? namingLogEvent,string callBackUrl);
    public Task SendMessageAsync(Guid groupId,VoteCharmingCompleteEvent? namingLogEvent,string callBackUrl);
}