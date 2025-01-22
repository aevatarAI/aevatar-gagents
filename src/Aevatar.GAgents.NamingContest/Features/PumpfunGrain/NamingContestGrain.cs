using Aevatar.GAgents.NamingContest.Common;
using Aevatar.GAgents.NamingContest.Features.Provider;
using AiSmart.GAgent.NamingContest.VoteAgent;
using Orleans.Providers;

namespace Aevatar.GAgents.NamingContest.Features.PumpfunGrain;


[StorageProvider(ProviderName = "PubSubStore")]
public class NamingContestGrain : Grain, INamingContestGrain
{
    private readonly INameContestProvider _nameContestProvider;
    
    public NamingContestGrain(INameContestProvider nameContestProvider) 
    {
        _nameContestProvider = nameContestProvider;
    }

    public async Task SendMessageAsync(Guid groupId,NamingLogGEvent? nameContentGEvent,string callBackUrl)
    {
        
        if (nameContentGEvent != null)
        {
            await _nameContestProvider.SendMessageAsync(groupId,nameContentGEvent,callBackUrl);
        }
    }

    public async Task SendMessageAsync(Guid groupId, VoteCharmingCompleteEvent? nameContentGEvent, string callBackUrl)
    {
        if (nameContentGEvent != null)
        {
            await _nameContestProvider.SendMessageAsync(groupId,nameContentGEvent,callBackUrl);
        }
    }
}