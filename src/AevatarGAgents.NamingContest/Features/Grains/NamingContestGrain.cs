using System;
using System.Threading.Tasks;
using AevatarGAgents.NamingContest.Common;
using AevatarGAgents.NamingContest.Provider;
using AevatarGAgents.NamingContest.VoteGAgent;
using Orleans;
using Orleans.Providers;

namespace AevatarGAgents.NamingContest.Grains;

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