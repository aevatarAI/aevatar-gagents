using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AevatarGAgents.Twitter.Dto;
using AevatarGAgents.Twitter.Options;
using AevatarGAgents.Twitter.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Providers;

namespace AevatarGAgents.Twitter.Grains;

[StorageProvider(ProviderName = "PubSubStore")]
public class TwitterGrain : Grain<TwitterState>, ITwitterGrain
{
    private readonly ITwitterProvider _twitterProvider;
    private ILogger<TwitterGrain> _logger;
    private readonly IOptionsMonitor<TwitterOptions> _twitterOptions;
    
    public TwitterGrain(ITwitterProvider twitterProvider, 
        ILogger<TwitterGrain> logger, 
        IOptionsMonitor<TwitterOptions> twitterOptions) 
    {
        _twitterProvider = twitterProvider;
        _logger = logger;
        _twitterOptions = twitterOptions;
    }
    
    public async Task CreateTweetAsync(string text, string token, string tokenSecret)
    {
        await _twitterProvider.PostTwitterAsync(text, token, tokenSecret);
    }
    
    public async Task ReplyTweetAsync(string text, string tweetId, string token, string tokenSecret)
    {
        await _twitterProvider.ReplyAsync(text, tweetId, token, tokenSecret);
    }

    public async Task<List<Tweet>> GetRecentMentionAsync(string userName)
    {
        var mentionList = await _twitterProvider.GetMentionsAsync(userName);
        return mentionList.Take(_twitterOptions.CurrentValue.ReplyLimit).ToList();
    }
}