using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.VoteAgent;

namespace Aevatar.GAgents.NamingContest.Features.Provider;

public interface INameContestProvider
{
    public Task SendMessageAsync(Guid groupId, NamingLogGEvent? namingLogEvent, string callBackUrl);
    public Task SendMessageAsync(Guid groupId, VoteCharmingCompleteEvent? namingLogEvent, string callBackUrl);
}