using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.Twitter.Dto;
using Orleans;

namespace Aevatar.GAgents.Twitter.Grains;

public interface ITwitterGrain : IGrainWithStringKey
{
    public Task CreateTweetAsync(string text, string token, string tokenSecret);
    public Task ReplyTweetAsync(string text, string tweetId, string token, string tokenSecret);
    public Task<List<Tweet>> GetRecentMentionAsync(string userName);
}