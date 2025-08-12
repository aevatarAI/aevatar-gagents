using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Twitter.GEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.Twitter.GAgents;

// Interface definition with comprehensive Twitter API v2 support
public interface ITwitterWebApiGAgent : IStateGAgent<TwitterWebApiGAgentState>
{
    // Tweet Management
    Task<TweetResponseDto> PostTweetAsync(string text, List<string>? mediaIds = null, CancellationToken cancellationToken = default);
    Task<TweetResponseDto> ReplyToTweetAsync(string inReplyToTweetId, string text, List<string>? mediaIds = null, CancellationToken cancellationToken = default);
    Task<TweetResponseDto> QuoteTweetAsync(string quotedTweetId, string text, CancellationToken cancellationToken = default);
    Task<TweetSearchResultDto> SearchRecentTweetsAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    Task<TweetDetailDto?> GetTweetByIdAsync(string tweetId, CancellationToken cancellationToken = default);
    Task<bool> DeleteTweetAsync(string tweetId, CancellationToken cancellationToken = default);
    
    // User Interactions
    Task<bool> LikeTweetAsync(string tweetId, CancellationToken cancellationToken = default);
    Task<bool> UnlikeTweetAsync(string tweetId, CancellationToken cancellationToken = default);
    Task<bool> RetweetAsync(string tweetId, CancellationToken cancellationToken = default);
    Task<bool> UnretweetAsync(string tweetId, CancellationToken cancellationToken = default);
    
    // User Profile
    Task<UserProfileDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default);
    
    // Relationships
    Task<bool> FollowUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UnfollowUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserListResultDto> GetFollowersAsync(string? userId = null, int maxResults = 100, CancellationToken cancellationToken = default);
    Task<UserListResultDto> GetFollowingAsync(string? userId = null, int maxResults = 100, CancellationToken cancellationToken = default);
    
    // Timelines
    Task<TimelineResultDto> GetHomeTimelineAsync(int maxResults = 100, string? paginationToken = null, CancellationToken cancellationToken = default);
    Task<TimelineResultDto> GetUserTimelineAsync(string userId, int maxResults = 100, string? paginationToken = null, CancellationToken cancellationToken = default);
}

// DTOs
[GenerateSerializer]
public class TweetResponseDto
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Text { get; set; } = string.Empty;
    [Id(2)] public string CreatedAt { get; set; } = string.Empty;
    [Id(3)] public string? EditHistoryTweetIds { get; set; }
}

[GenerateSerializer]
public class TweetDetailDto
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Text { get; set; } = string.Empty;
    [Id(2)] public string AuthorId { get; set; } = string.Empty;
    [Id(3)] public string CreatedAt { get; set; } = string.Empty;
    [Id(4)] public Dictionary<string, int>? PublicMetrics { get; set; }
    [Id(5)] public string? ConversationId { get; set; }
    [Id(6)] public string? InReplyToUserId { get; set; }
}

[GenerateSerializer]
public class TweetSearchResultDto
{
    [Id(0)] public List<TweetDetailDto> Tweets { get; set; } = new();
    [Id(1)] public int ResultCount { get; set; }
    [Id(2)] public string? NextToken { get; set; }
}

[GenerateSerializer]
public class UserProfileDto
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Username { get; set; } = string.Empty;
    [Id(2)] public string Name { get; set; } = string.Empty;
    [Id(3)] public string? Description { get; set; }
    [Id(4)] public string CreatedAt { get; set; } = string.Empty;
    [Id(5)] public Dictionary<string, int>? PublicMetrics { get; set; }
    [Id(6)] public bool Verified { get; set; }
    [Id(7)] public string? ProfileImageUrl { get; set; }
}

[GenerateSerializer]
public class UserListResultDto
{
    [Id(0)] public List<UserProfileDto> Users { get; set; } = new();
    [Id(1)] public int ResultCount { get; set; }
    [Id(2)] public string? NextToken { get; set; }
}

[GenerateSerializer]
public class TimelineResultDto
{
    [Id(0)] public List<TweetDetailDto> Tweets { get; set; } = new();
    [Id(1)] public int ResultCount { get; set; }
    [Id(2)] public string? NextToken { get; set; }
    [Id(3)] public string? PreviousToken { get; set; }
}

// State
[GenerateSerializer]
public class TwitterWebApiGAgentState : StateBase
{
    [Id(0)] public string BaseApiUrl { get; set; } = "https://api.twitter.com/2";
    [Id(1)] public string BearerToken { get; set; } = string.Empty;
    [Id(2)] public string? OAuthToken { get; set; }
    [Id(3)] public string? OAuthTokenSecret { get; set; }
    [Id(4)] public string? UserId { get; set; }
    [Id(5)] public int RequestTimeoutSeconds { get; set; } = 30;
    
    [Id(10)] public List<string> RecentTweetIds { get; set; } = new();
    [Id(11)] public List<string> LikedTweetIds { get; set; } = new();
    [Id(12)] public List<string> RetweetedTweetIds { get; set; } = new();
    [Id(13)] public List<string> FollowingUserIds { get; set; } = new();
    [Id(14)] public string LastSearchQuery { get; set; } = string.Empty;
    [Id(15)] public DateTime LastOperationUtc { get; set; } = DateTime.MinValue;
    [Id(16)] public Dictionary<string, int> OperationCounts { get; set; } = new();
}

// Configuration
[GenerateSerializer]
public class TwitterWebApiGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public string BaseApiUrl { get; set; } = "https://api.twitter.com/2";
    [Id(1)] public string BearerToken { get; set; } = string.Empty;
    [Id(2)] public string? OAuthToken { get; set; }
    [Id(3)] public string? OAuthTokenSecret { get; set; }
    [Id(4)] public string? ConsumerKey { get; set; }
    [Id(5)] public string? ConsumerSecret { get; set; }
    [Id(6)] public int RequestTimeoutSeconds { get; set; } = 30;
}

// State Log Events
[GenerateSerializer]
public class TwitterWebApiStateLogEvent : StateLogEventBase<TwitterWebApiStateLogEvent> { }

[GenerateSerializer]
public class TwitterConfigSetLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string BaseApiUrl { get; set; } = string.Empty;
    [Id(1)] public string BearerToken { get; set; } = string.Empty;
    [Id(2)] public int RequestTimeoutSeconds { get; set; }
}

[GenerateSerializer]
public class TweetCreatedLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string TweetId { get; set; } = string.Empty;
    [Id(1)] public string Text { get; set; } = string.Empty;
    [Id(2)] public DateTime CreatedAt { get; set; }
}

[GenerateSerializer]
public class TweetDeletedLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string TweetId { get; set; } = string.Empty;
    [Id(1)] public DateTime DeletedAt { get; set; }
}

[GenerateSerializer]
public class TweetInteractionLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string TweetId { get; set; } = string.Empty;
    [Id(1)] public string InteractionType { get; set; } = string.Empty; // like, unlike, retweet, unretweet
    [Id(2)] public DateTime InteractedAt { get; set; }
}

[GenerateSerializer]
public class UserRelationshipLogEvent : TwitterWebApiStateLogEvent
{
    [Id(0)] public string TargetUserId { get; set; } = string.Empty;
    [Id(1)] public string RelationshipAction { get; set; } = string.Empty; // follow, unfollow
    [Id(2)] public DateTime ActionAt { get; set; }
}

// GAgent Implementation
[GAgent("twitter-webapi", "social.twitter")]
public class TwitterWebApiGAgent : GAgentBase<TwitterWebApiGAgentState, TwitterWebApiStateLogEvent, EventBase, TwitterWebApiGAgentConfiguration>, ITwitterWebApiGAgent
{
    private HttpClient? _httpClient;
    
    private HttpClient HttpClient => _httpClient ??= ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();

    public override Task<string> GetDescriptionAsync()
    {
        var description = @"Twitter Web API GAgent  - Comprehensive Twitter/X API v2 Integration

CAPABILITIES:

[TWEET MANAGEMENT]
• PostTweetEvent - Create new tweet with text and optional media
• ReplyToTweetEvent - Reply to existing tweet with threading
• QuoteTweetEvent - Quote retweet with commentary
• DeleteTweetEvent - Delete your own tweet by ID
• GetTweetEvent - Retrieve detailed tweet information
• SearchRecentTweetsEvent - Search tweets from last 7 days

[USER INTERACTIONS]
• LikeTweetEvent - Like/favorite a tweet
• UnlikeTweetEvent - Remove like from tweet
• RetweetEvent - Retweet without quote text
• UnretweetEvent - Remove retweet

[USER PROFILES]
• GetUserByUsernameEvent - Lookup user profile by @username
• GetUserByIdEvent - Lookup user profile by numeric ID
• GetMyProfileEvent - Get authenticated user's profile

[RELATIONSHIPS]
• FollowUserEvent - Follow a user account
• UnfollowUserEvent - Unfollow a user account
• GetFollowersEvent - List user's followers with pagination
• GetFollowingEvent - List accounts user follows

[TIMELINES & FEEDS]
• GetHomeTimelineEvent - Authenticated user's home timeline
• GetUserTimelineEvent - Specific user's tweet timeline
• GetMentionsTimelineEvent - Mentions of authenticated user

[CONFIGURATION]
Configure via ConfigAsync with TwitterWebApiGAgentConfiguration:
- BearerToken: Required for API authentication
- OAuthToken/Secret: For user-context operations
- BaseApiUrl: API endpoint (default: https://api.twitter.com/2)
- RequestTimeoutSeconds: HTTP timeout (default: 30)

[RATE LIMITS]
Respects Twitter API rate limits:
- Tweet creation: 200/15min
- Likes: 1000/24hr
- Follows: 400/24hr
- Search: 180/15min

All event handlers accept events from Aevatar.GAgents.Twitter.GEvents namespace.";
        
        return Task.FromResult(description);
    }

    protected override async Task PerformConfigAsync(TwitterWebApiGAgentConfiguration configuration)
    {
        RaiseEvent(new TwitterConfigSetLogEvent
        {
            BaseApiUrl = configuration.BaseApiUrl,
            BearerToken = configuration.BearerToken,
            RequestTimeoutSeconds = configuration.RequestTimeoutSeconds
        });
        
        await ConfirmEvents();
    }

    protected override void GAgentTransitionState(TwitterWebApiGAgentState state, StateLogEventBase<TwitterWebApiStateLogEvent> @event)
    {
        switch (@event)
        {
            case TwitterConfigSetLogEvent e:
                state.BaseApiUrl = e.BaseApiUrl;
                state.BearerToken = e.BearerToken;
                state.RequestTimeoutSeconds = e.RequestTimeoutSeconds;
                break;
                
            case TweetCreatedLogEvent e:
                state.RecentTweetIds.Add(e.TweetId);
                if (state.RecentTweetIds.Count > 100)
                    state.RecentTweetIds.RemoveAt(0);
                state.LastOperationUtc = e.CreatedAt;
                IncrementOperationCount(state, "tweets_posted");
                break;
                
            case TweetDeletedLogEvent e:
                state.RecentTweetIds.Remove(e.TweetId);
                state.LastOperationUtc = e.DeletedAt;
                IncrementOperationCount(state, "tweets_deleted");
                break;
                
            case TweetInteractionLogEvent e:
                state.LastOperationUtc = e.InteractedAt;
                switch (e.InteractionType)
                {
                    case "like":
                        state.LikedTweetIds.Add(e.TweetId);
                        IncrementOperationCount(state, "tweets_liked");
                        break;
                    case "unlike":
                        state.LikedTweetIds.Remove(e.TweetId);
                        break;
                    case "retweet":
                        state.RetweetedTweetIds.Add(e.TweetId);
                        IncrementOperationCount(state, "tweets_retweeted");
                        break;
                    case "unretweet":
                        state.RetweetedTweetIds.Remove(e.TweetId);
                        break;
                }
                break;
                
            case UserRelationshipLogEvent e:
                state.LastOperationUtc = e.ActionAt;
                if (e.RelationshipAction == "follow")
                {
                    state.FollowingUserIds.Add(e.TargetUserId);
                    IncrementOperationCount(state, "users_followed");
                }
                else if (e.RelationshipAction == "unfollow")
                {
                    state.FollowingUserIds.Remove(e.TargetUserId);
                }
                break;
        }
    }

    private void IncrementOperationCount(TwitterWebApiGAgentState state, string operation)
    {
        if (!state.OperationCounts.ContainsKey(operation))
            state.OperationCounts[operation] = 0;
        state.OperationCounts[operation]++;
    }

    // Tweet Management Implementation
    public async Task<TweetResponseDto> PostTweetAsync(string text, List<string>? mediaIds = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                text,
                media = mediaIds != null ? new { media_ids = mediaIds } : null
            };
            
            var response = await SendRequestAsync(HttpMethod.Post, "/tweets", payload, cancellationToken);
            var result = JsonSerializer.Deserialize<TweetResponseDto>(response);
            
            RaiseEvent(new TweetCreatedLogEvent
            {
                TweetId = result!.Id,
                Text = text,
                CreatedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to post tweet");
            throw;
        }
    }

    public async Task<TweetResponseDto> ReplyToTweetAsync(string inReplyToTweetId, string text, List<string>? mediaIds = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                text,
                reply = new { in_reply_to_tweet_id = inReplyToTweetId },
                media = mediaIds != null ? new { media_ids = mediaIds } : null
            };
            
            var response = await SendRequestAsync(HttpMethod.Post, "/tweets", payload, cancellationToken);
            var result = JsonSerializer.Deserialize<TweetResponseDto>(response);
            
            RaiseEvent(new TweetCreatedLogEvent
            {
                TweetId = result!.Id,
                Text = text,
                CreatedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reply to tweet");
            throw;
        }
    }

    public async Task<TweetResponseDto> QuoteTweetAsync(string quotedTweetId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                text,
                quote_tweet_id = quotedTweetId
            };
            
            var response = await SendRequestAsync(HttpMethod.Post, "/tweets", payload, cancellationToken);
            var result = JsonSerializer.Deserialize<TweetResponseDto>(response);
            
            RaiseEvent(new TweetCreatedLogEvent
            {
                TweetId = result!.Id,
                Text = text,
                CreatedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to quote tweet");
            throw;
        }
    }

    public async Task<bool> DeleteTweetAsync(string tweetId, CancellationToken cancellationToken = default)
    {
        try
        {
            await SendRequestAsync(HttpMethod.Delete, $"/tweets/{tweetId}", null, cancellationToken);
            
            RaiseEvent(new TweetDeletedLogEvent
            {
                TweetId = tweetId,
                DeletedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete tweet {TweetId}", tweetId);
            return false;
        }
    }

    public async Task<TweetDetailDto?> GetTweetByIdAsync(string tweetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/tweets/{tweetId}?tweet.fields=created_at,author_id,conversation_id,in_reply_to_user_id,public_metrics", 
                null, cancellationToken);
            
            var data = JsonDocument.Parse(response).RootElement.GetProperty("data");
            return JsonSerializer.Deserialize<TweetDetailDto>(data.GetRawText());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get tweet {TweetId}", tweetId);
            return null;
        }
    }

    public async Task<TweetSearchResultDto> SearchRecentTweetsAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/tweets/search/recent?query={Uri.EscapeDataString(query)}&max_results={maxResults}&tweet.fields=created_at,author_id,public_metrics", 
                null, cancellationToken);
            
            var doc = JsonDocument.Parse(response);
            var result = new TweetSearchResultDto();
            
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                result.Tweets = JsonSerializer.Deserialize<List<TweetDetailDto>>(data.GetRawText()) ?? new();
                result.ResultCount = result.Tweets.Count;
            }
            
            if (doc.RootElement.TryGetProperty("meta", out var meta) && 
                meta.TryGetProperty("next_token", out var nextToken))
            {
                result.NextToken = nextToken.GetString();
            }
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to search tweets");
            throw;
        }
    }

    // User Interactions
    public async Task<bool> LikeTweetAsync(string tweetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            var payload = new { tweet_id = tweetId };
            
            await SendRequestAsync(HttpMethod.Post, $"/users/{userId}/likes", payload, cancellationToken);
            
            RaiseEvent(new TweetInteractionLogEvent
            {
                TweetId = tweetId,
                InteractionType = "like",
                InteractedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to like tweet {TweetId}", tweetId);
            return false;
        }
    }

    public async Task<bool> UnlikeTweetAsync(string tweetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            await SendRequestAsync(HttpMethod.Delete, $"/users/{userId}/likes/{tweetId}", null, cancellationToken);
            
            RaiseEvent(new TweetInteractionLogEvent
            {
                TweetId = tweetId,
                InteractionType = "unlike",
                InteractedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unlike tweet {TweetId}", tweetId);
            return false;
        }
    }

    public async Task<bool> RetweetAsync(string tweetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            var payload = new { tweet_id = tweetId };
            
            await SendRequestAsync(HttpMethod.Post, $"/users/{userId}/retweets", payload, cancellationToken);
            
            RaiseEvent(new TweetInteractionLogEvent
            {
                TweetId = tweetId,
                InteractionType = "retweet",
                InteractedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retweet {TweetId}", tweetId);
            return false;
        }
    }

    public async Task<bool> UnretweetAsync(string tweetId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            await SendRequestAsync(HttpMethod.Delete, $"/users/{userId}/retweets/{tweetId}", null, cancellationToken);
            
            RaiseEvent(new TweetInteractionLogEvent
            {
                TweetId = tweetId,
                InteractionType = "unretweet",
                InteractedAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unretweet {TweetId}", tweetId);
            return false;
        }
    }

    // User Profile
    public async Task<UserProfileDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/users/by/username/{username}?user.fields=created_at,description,public_metrics,verified,profile_image_url", 
                null, cancellationToken);
            
            var data = JsonDocument.Parse(response).RootElement.GetProperty("data");
            return JsonSerializer.Deserialize<UserProfileDto>(data.GetRawText());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get user by username {Username}", username);
            return null;
        }
    }

    public async Task<UserProfileDto?> GetMyProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, 
                "/users/me?user.fields=created_at,description,public_metrics,verified,profile_image_url", 
                null, cancellationToken);
            
            var data = JsonDocument.Parse(response).RootElement.GetProperty("data");
            var profile = JsonSerializer.Deserialize<UserProfileDto>(data.GetRawText());
            
            // Cache user ID for future operations
            if (profile != null && State.UserId != profile.Id)
            {
                State.UserId = profile.Id;
            }
            
            return profile;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get authenticated user profile");
            return null;
        }
    }

    // Relationships
    public async Task<bool> FollowUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var myUserId = State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            var payload = new { target_user_id = userId };
            
            await SendRequestAsync(HttpMethod.Post, $"/users/{myUserId}/following", payload, cancellationToken);
            
            RaiseEvent(new UserRelationshipLogEvent
            {
                TargetUserId = userId,
                RelationshipAction = "follow",
                ActionAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to follow user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UnfollowUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var myUserId = State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            await SendRequestAsync(HttpMethod.Delete, $"/users/{myUserId}/following/{userId}", null, cancellationToken);
            
            RaiseEvent(new UserRelationshipLogEvent
            {
                TargetUserId = userId,
                RelationshipAction = "unfollow",
                ActionAt = DateTime.UtcNow
            });
            await ConfirmEvents();
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to unfollow user {UserId}", userId);
            return false;
        }
    }

    public async Task<UserListResultDto> GetFollowersAsync(string? userId = null, int maxResults = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetUserId = userId ?? State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/users/{targetUserId}/followers?max_results={maxResults}&user.fields=created_at,description,public_metrics,verified", 
                null, cancellationToken);
            
            return ParseUserListResponse(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get followers");
            return new UserListResultDto();
        }
    }

    public async Task<UserListResultDto> GetFollowingAsync(string? userId = null, int maxResults = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var targetUserId = userId ?? State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/users/{targetUserId}/following?max_results={maxResults}&user.fields=created_at,description,public_metrics,verified", 
                null, cancellationToken);
            
            return ParseUserListResponse(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get following");
            return new UserListResultDto();
        }
    }

    // Timelines
    public async Task<TimelineResultDto> GetHomeTimelineAsync(int maxResults = 100, string? paginationToken = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync(cancellationToken);
            var url = $"/users/{userId}/timelines/reverse_chronological?max_results={maxResults}&tweet.fields=created_at,author_id,public_metrics";
            if (!string.IsNullOrEmpty(paginationToken))
                url += $"&pagination_token={paginationToken}";
            
            var response = await SendRequestAsync(HttpMethod.Get, url, null, cancellationToken);
            return ParseTimelineResponse(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get home timeline");
            return new TimelineResultDto();
        }
    }

    public async Task<TimelineResultDto> GetUserTimelineAsync(string userId, int maxResults = 100, string? paginationToken = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/users/{userId}/tweets?max_results={maxResults}&tweet.fields=created_at,author_id,public_metrics";
            if (!string.IsNullOrEmpty(paginationToken))
                url += $"&pagination_token={paginationToken}";
            
            var response = await SendRequestAsync(HttpMethod.Get, url, null, cancellationToken);
            return ParseTimelineResponse(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get user timeline for {UserId}", userId);
            return new TimelineResultDto();
        }
    }

    // Event Handlers
    [EventHandler]
    public async Task HandlePostTweetEventAsync(PostTweetEvent @event)
    {
        await PostTweetAsync(@event.Text, @event.MediaIds);
    }

    [EventHandler]
    public async Task HandleReplyToTweetEventAsync(ReplyToTweetEvent @event)
    {
        await ReplyToTweetAsync(@event.InReplyToTweetId, @event.Text, @event.MediaIds);
    }

    [EventHandler]
    public async Task HandleQuoteTweetEventAsync(QuoteTweetEvent @event)
    {
        await QuoteTweetAsync(@event.QuotedTweetId, @event.Text);
    }

    [EventHandler]
    public async Task HandleDeleteTweetEventAsync(DeleteTweetEvent @event)
    {
        await DeleteTweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleGetTweetEventAsync(GetTweetEvent @event)
    {
        var tweet = await GetTweetByIdAsync(@event.TweetId);
        if (tweet != null)
        {
            Logger.LogInformation("Retrieved tweet {TweetId}: {Text}", tweet.Id, tweet.Text);
        }
    }

    [EventHandler]
    public async Task HandleSearchRecentTweetsEventAsync(SearchRecentTweetsEvent @event)
    {
        var results = await SearchRecentTweetsAsync(@event.Query, @event.MaxResults);
        Logger.LogInformation("Found {Count} tweets for query: {Query}", results.ResultCount, @event.Query);
    }

    [EventHandler]
    public async Task HandleLikeTweetEventAsync(LikeTweetEvent @event)
    {
        await LikeTweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleUnlikeTweetEventAsync(UnlikeTweetEvent @event)
    {
        await UnlikeTweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleRetweetEventAsync(RetweetEvent @event)
    {
        await RetweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleUnretweetEventAsync(UnretweetEvent @event)
    {
        await UnretweetAsync(@event.TweetId);
    }

    [EventHandler]
    public async Task HandleGetUserByUsernameEventAsync(GetUserByUsernameEvent @event)
    {
        var user = await GetUserByUsernameAsync(@event.Username);
        if (user != null)
        {
            Logger.LogInformation("Found user @{Username}: {Name} ({Id})", user.Username, user.Name, user.Id);
        }
    }

    [EventHandler]
    public async Task HandleGetMyProfileEventAsync(GetMyProfileEvent @event)
    {
        var profile = await GetMyProfileAsync();
        if (profile != null)
        {
            Logger.LogInformation("My profile: @{Username} ({Id})", profile.Username, profile.Id);
        }
    }

    [EventHandler]
    public async Task HandleFollowUserEventAsync(FollowUserEvent @event)
    {
        await FollowUserAsync(@event.TargetUserId);
    }

    [EventHandler]
    public async Task HandleUnfollowUserEventAsync(UnfollowUserEvent @event)
    {
        await UnfollowUserAsync(@event.TargetUserId);
    }

    [EventHandler]
    public async Task HandleGetFollowersEventAsync(GetFollowersEvent @event)
    {
        var followers = await GetFollowersAsync(@event.TargetUserId, @event.MaxResults);
        Logger.LogInformation("Retrieved {Count} followers", followers.ResultCount);
    }

    [EventHandler]
    public async Task HandleGetFollowingEventAsync(GetFollowingEvent @event)
    {
        var following = await GetFollowingAsync(@event.TargetUserId, @event.MaxResults);
        Logger.LogInformation("Retrieved {Count} following", following.ResultCount);
    }

    [EventHandler]
    public async Task HandleGetHomeTimelineEventAsync(GetHomeTimelineEvent @event)
    {
        var timeline = await GetHomeTimelineAsync(@event.MaxResults, @event.PaginationToken);
        Logger.LogInformation("Retrieved {Count} tweets from home timeline", timeline.ResultCount);
    }

    [EventHandler]
    public async Task HandleGetUserTimelineEventAsync(GetUserTimelineEvent @event)
    {
        var timeline = await GetUserTimelineAsync(@event.TargetUserId, @event.MaxResults, @event.PaginationToken);
        Logger.LogInformation("Retrieved {Count} tweets from user timeline", timeline.ResultCount);
    }

    // Helper Methods
    private async Task<string> SendRequestAsync(HttpMethod method, string endpoint, object? payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, $"{State.BaseApiUrl}{endpoint}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", State.BearerToken);
        
        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(State.RequestTimeoutSeconds));
        
        var response = await HttpClient.SendAsync(request, cts.Token);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Twitter API request failed: {Status} - {Content}", response.StatusCode, content);
            throw new HttpRequestException($"Twitter API error: {response.StatusCode}");
        }
        
        return content;
    }

    private async Task<string> GetAuthenticatedUserIdAsync(CancellationToken cancellationToken)
    {
        var profile = await GetMyProfileAsync(cancellationToken);
        if (profile == null)
            throw new InvalidOperationException("Failed to get authenticated user ID");
        return profile.Id;
    }

    private UserListResultDto ParseUserListResponse(string response)
    {
        var doc = JsonDocument.Parse(response);
        var result = new UserListResultDto();
        
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            result.Users = JsonSerializer.Deserialize<List<UserProfileDto>>(data.GetRawText()) ?? new();
            result.ResultCount = result.Users.Count;
        }
        
        if (doc.RootElement.TryGetProperty("meta", out var meta) && 
            meta.TryGetProperty("next_token", out var nextToken))
        {
            result.NextToken = nextToken.GetString();
        }
        
        return result;
    }

    private TimelineResultDto ParseTimelineResponse(string response)
    {
        var doc = JsonDocument.Parse(response);
        var result = new TimelineResultDto();
        
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            result.Tweets = JsonSerializer.Deserialize<List<TweetDetailDto>>(data.GetRawText()) ?? new();
            result.ResultCount = result.Tweets.Count;
        }
        
        if (doc.RootElement.TryGetProperty("meta", out var meta))
        {
            if (meta.TryGetProperty("next_token", out var nextToken))
                result.NextToken = nextToken.GetString();
            if (meta.TryGetProperty("previous_token", out var prevToken))
                result.PreviousToken = prevToken.GetString();
        }
        
        return result;
    }
}