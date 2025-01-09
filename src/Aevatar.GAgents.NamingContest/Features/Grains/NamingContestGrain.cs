using System;
using System.Threading.Tasks;
using Aevatar.GAgents.NamingContest.Common;
using Aevatar.GAgents.NamingContest.Provider;
using Aevatar.GAgents.NamingContest.VoteGAgent;
using Orleans;
using Orleans.Providers;

namespace Aevatar.GAgents.NamingContest.Grains;

[StorageProvider(ProviderName = "PubSubStore")]
public class NamingContestGrain : Grain<NamingContestState>, INamingContestGrain
{
    private readonly INameContestProvider _nameContestProvider;
    
    public NamingContestGrain(INameContestProvider nameContestProvider) 
    {
        _nameContestProvider = nameContestProvider;
    }

    public async Task SendMessageAsync(Guid groupId,NamingAILogEvent? nameContentGEvent,string callBackUrl)
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