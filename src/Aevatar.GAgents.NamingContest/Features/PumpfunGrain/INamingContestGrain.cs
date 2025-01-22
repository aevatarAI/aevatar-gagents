using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.VoteAgent;

namespace Aevatar.GAgents.NamingContest.Features.PumpfunGrain;

public interface INamingContestGrain : IGrainWithStringKey
{
    public Task SendMessageAsync(Guid groupId,NamingLogGEvent? nameContentGEvent,string callBackUrl);
    public Task SendMessageAsync(Guid groupId,VoteCharmingCompleteEvent? nameContentGEvent,string callBackUrl);
}