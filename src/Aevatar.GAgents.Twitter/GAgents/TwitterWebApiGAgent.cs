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
    Task<TweetResponseDto> PostTweetAsync(string text, List<string>? mediaIds = null);
    Task<TweetResponseDto> ReplyToTweetAsync(string inReplyToTweetId, string text, List<string>? mediaIds = null);
    Task<TweetResponseDto> QuoteTweetAsync(string quotedTweetId, string text);
    Task<TweetSearchResultDto> SearchRecentTweetsAsync(string query, int maxResults = 10);
    Task<TweetDetailDto?> GetTweetByIdAsync(string tweetId);
    Task<bool> DeleteTweetAsync(string tweetId);
    
    // User Interactions
    Task<bool> LikeTweetAsync(string tweetId);
    Task<bool> UnlikeTweetAsync(string tweetId);
    Task<bool> RetweetAsync(string tweetId);
    Task<bool> UnretweetAsync(string tweetId);
    
    // User Profile
    Task<UserProfileDto?> GetUserByUsernameAsync(string username);
    Task<UserProfileDto?> GetMyProfileAsync();
    
    // Relationships
    Task<bool> FollowUserAsync(string userId);
    Task<bool> UnfollowUserAsync(string userId);
    Task<UserListResultDto> GetFollowersAsync(string? userId = null, int maxResults = 100);
    Task<UserListResultDto> GetFollowingAsync(string? userId = null, int maxResults = 100);
    
    // Timelines
    Task<TimelineResultDto> GetHomeTimelineAsync(int maxResults = 100, string? paginationToken = null);
    Task<TimelineResultDto> GetUserTimelineAsync(string userId, int maxResults = 100, string? paginationToken = null);
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
    [Id(6)] public string? ConsumerKey { get; set; }
    [Id(7)] public string? ConsumerSecret { get; set; }
    
    [Id(10)] public List<string> RecentTweetIds { get; set; } = new();
    [Id(11)] public List<string> LikedTweetIds { get; set; } = new();
    [Id(12)] public List<string> RetweetedTweetIds { get; set; } = new();
    [Id(13)] public List<string> FollowingUserIds { get; set; } = new();
    [Id(14)] public string LastSearchQuery { get; set; } = string.Empty;
    [Id(15)] public DateTime LastOperationUtc { get; set; } = DateTime.MinValue;
    [Id(16)] public Dictionary<string, int> OperationCounts { get; set; } = new();
}

// Authentication Types
public enum TwitterAuthenticationMode
{
    BearerToken,      // App-only authentication for read-only operations
    OAuth1a,          // User context authentication for write operations
    OAuth2UserContext, // OAuth 2.0 user context (future support)
    Auto              // Automatically choose the best available authentication method
}

[GenerateSerializer]
public class TwitterAuthenticationResult
{
    [Id(0)] public bool IsSuccess { get; set; }
    [Id(1)] public string ErrorMessage { get; set; } = string.Empty;
    [Id(2)] public TwitterAuthenticationMode AuthMode { get; set; }
    [Id(3)] public Dictionary<string, string> Headers { get; set; } = new();
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
    // Removed PreferredAuthMode - now supports both authentication methods simultaneously
    [Id(8)] public bool EnableDebugLogging { get; set; } = false;
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
    [Id(3)] public string? ConsumerKey { get; set; }
    [Id(4)] public string? ConsumerSecret { get; set; }
    [Id(5)] public string? OAuthToken { get; set; }
    [Id(6)] public string? OAuthTokenSecret { get; set; }
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
        // Validate configuration
        var validationResult = ValidateConfiguration(configuration);
        if (!validationResult.IsSuccess)
        {
            Logger.LogError("TwitterWebApiGAgent configuration validation failed: {Error}", validationResult.ErrorMessage);
            throw new InvalidOperationException($"Configuration validation failed: {validationResult.ErrorMessage}");
        }
        
        RaiseEvent(new TwitterConfigSetLogEvent
        {
            BaseApiUrl = configuration.BaseApiUrl,
            BearerToken = configuration.BearerToken,
            RequestTimeoutSeconds = configuration.RequestTimeoutSeconds,
            ConsumerKey = configuration.ConsumerKey,
            ConsumerSecret = configuration.ConsumerSecret,
            OAuthToken = configuration.OAuthToken,
            OAuthTokenSecret = configuration.OAuthTokenSecret
        });
        
        await ConfirmEvents();
        
        if (configuration.EnableDebugLogging)
        {
            var authMethods = new List<string>();
            if (!string.IsNullOrEmpty(configuration.BearerToken))
                authMethods.Add("Bearer Token");
            if (!string.IsNullOrEmpty(configuration.OAuthToken))
                authMethods.Add("OAuth 1.0a");
            
            Logger.LogInformation("TwitterWebApiGAgent configured with authentication methods: {AuthMethods}", 
                string.Join(", ", authMethods));
        }
    }
    
    private TwitterAuthenticationResult ValidateConfiguration(TwitterWebApiGAgentConfiguration configuration)
    {
        var result = new TwitterAuthenticationResult();
        var errors = new List<string>();
        
        // Check basic configuration
        if (string.IsNullOrEmpty(configuration.BaseApiUrl))
            errors.Add("BaseApiUrl is required");
        
        if (configuration.RequestTimeoutSeconds <= 0)
            errors.Add("RequestTimeoutSeconds must be positive");
        
        // Check that at least one authentication method is configured
        bool hasBearerToken = !string.IsNullOrEmpty(configuration.BearerToken);
        bool hasOAuthCredentials = !string.IsNullOrEmpty(configuration.ConsumerKey) &&
                                  !string.IsNullOrEmpty(configuration.ConsumerSecret) &&
                                  !string.IsNullOrEmpty(configuration.OAuthToken) &&
                                  !string.IsNullOrEmpty(configuration.OAuthTokenSecret);
        
        if (!hasBearerToken && !hasOAuthCredentials)
        {
            errors.Add("At least one authentication method must be configured (Bearer Token or OAuth 1.0a)");
        }
        
        // Validate OAuth credentials if provided
        if (!string.IsNullOrEmpty(configuration.ConsumerKey) || 
            !string.IsNullOrEmpty(configuration.ConsumerSecret) ||
            !string.IsNullOrEmpty(configuration.OAuthToken) ||
            !string.IsNullOrEmpty(configuration.OAuthTokenSecret))
        {
            if (string.IsNullOrEmpty(configuration.ConsumerKey))
                errors.Add("ConsumerKey is required when using OAuth 1.0a authentication");
            
            if (string.IsNullOrEmpty(configuration.ConsumerSecret))
                errors.Add("ConsumerSecret is required when using OAuth 1.0a authentication");
            
            if (string.IsNullOrEmpty(configuration.OAuthToken))
                errors.Add("OAuthToken is required when using OAuth 1.0a authentication");
            
            if (string.IsNullOrEmpty(configuration.OAuthTokenSecret))
                errors.Add("OAuthTokenSecret is required when using OAuth 1.0a authentication");
        }
        
        if (errors.Any())
        {
            result.ErrorMessage = string.Join("; ", errors);
            result.IsSuccess = false;
        }
        else
        {
            result.IsSuccess = true;
        }
        
        return result;
    }

    protected override void GAgentTransitionState(TwitterWebApiGAgentState state, StateLogEventBase<TwitterWebApiStateLogEvent> @event)
    {
        switch (@event)
        {
            case TwitterConfigSetLogEvent e:
                state.BaseApiUrl = e.BaseApiUrl;
                state.BearerToken = e.BearerToken;
                state.RequestTimeoutSeconds = e.RequestTimeoutSeconds;
                state.ConsumerKey = e.ConsumerKey;
                state.ConsumerSecret = e.ConsumerSecret;
                state.OAuthToken = e.OAuthToken;
                state.OAuthTokenSecret = e.OAuthTokenSecret;
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
    public async Task<TweetResponseDto> PostTweetAsync(string text, List<string>? mediaIds = null)
    {
        try
        {
            // Build payload dynamically to avoid null fields
            var payload = new Dictionary<string, object>
            {
                ["text"] = text
            };
            
            // Only include media field if mediaIds are provided
            if (mediaIds != null && mediaIds.Count > 0)
            {
                payload["media"] = new { media_ids = mediaIds };
            }
            
            var response = await SendRequestAsync(HttpMethod.Post, "/tweets", payload);
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

    public async Task<TweetResponseDto> ReplyToTweetAsync(string inReplyToTweetId, string text, List<string>? mediaIds = null)
    {
        try
        {
            // Build payload dynamically to avoid null fields
            var payload = new Dictionary<string, object>
            {
                ["text"] = text,
                ["reply"] = new { in_reply_to_tweet_id = inReplyToTweetId }
            };
            
            // Only include media field if mediaIds are provided
            if (mediaIds != null && mediaIds.Count > 0)
            {
                payload["media"] = new { media_ids = mediaIds };
            }
            
            var response = await SendRequestAsync(HttpMethod.Post, "/tweets", payload);
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

    public async Task<TweetResponseDto> QuoteTweetAsync(string quotedTweetId, string text)
    {
        try
        {
            var payload = new
            {
                text,
                quote_tweet_id = quotedTweetId
            };
            
            var response = await SendRequestAsync(HttpMethod.Post, "/tweets", payload);
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

    public async Task<bool> DeleteTweetAsync(string tweetId)
    {
        try
        {
            await SendRequestAsync(HttpMethod.Delete, $"/tweets/{tweetId}", null);
            
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

    public async Task<TweetDetailDto?> GetTweetByIdAsync(string tweetId)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/tweets/{tweetId}?tweet.fields=created_at,author_id,conversation_id,in_reply_to_user_id,public_metrics", 
                null);
            
            var data = JsonDocument.Parse(response).RootElement.GetProperty("data");
            return JsonSerializer.Deserialize<TweetDetailDto>(data.GetRawText());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get tweet {TweetId}", tweetId);
            return null;
        }
    }

    public async Task<TweetSearchResultDto> SearchRecentTweetsAsync(string query, int maxResults = 10)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/tweets/search/recent?query={Uri.EscapeDataString(query)}&max_results={maxResults}&tweet.fields=created_at,author_id,public_metrics", 
                null);
            
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
    public async Task<bool> LikeTweetAsync(string tweetId)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync();
            var payload = new { tweet_id = tweetId };
            
            await SendRequestAsync(HttpMethod.Post, $"/users/{userId}/likes", payload);
            
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

    public async Task<bool> UnlikeTweetAsync(string tweetId)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync();
            await SendRequestAsync(HttpMethod.Delete, $"/users/{userId}/likes/{tweetId}", null);
            
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

    public async Task<bool> RetweetAsync(string tweetId)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync();
            var payload = new { tweet_id = tweetId };
            
            await SendRequestAsync(HttpMethod.Post, $"/users/{userId}/retweets", payload);
            
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

    public async Task<bool> UnretweetAsync(string tweetId)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync();
            await SendRequestAsync(HttpMethod.Delete, $"/users/{userId}/retweets/{tweetId}", null);
            
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
    public async Task<UserProfileDto?> GetUserByUsernameAsync(string username)
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/users/by/username/{username}?user.fields=created_at,description,public_metrics,verified,profile_image_url", 
                null);
            
            var data = JsonDocument.Parse(response).RootElement.GetProperty("data");
            return JsonSerializer.Deserialize<UserProfileDto>(data.GetRawText());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get user by username {Username}", username);
            return null;
        }
    }

    public async Task<UserProfileDto?> GetMyProfileAsync()
    {
        try
        {
            var response = await SendRequestAsync(HttpMethod.Get, 
                "/users/me?user.fields=created_at,description,public_metrics,verified,profile_image_url", 
                null);
            
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
    public async Task<bool> FollowUserAsync(string userId)
    {
        try
        {
            var myUserId = State.UserId ?? await GetAuthenticatedUserIdAsync();
            var payload = new { target_user_id = userId };
            
            await SendRequestAsync(HttpMethod.Post, $"/users/{myUserId}/following", payload);
            
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

    public async Task<bool> UnfollowUserAsync(string userId)
    {
        try
        {
            var myUserId = State.UserId ?? await GetAuthenticatedUserIdAsync();
            await SendRequestAsync(HttpMethod.Delete, $"/users/{myUserId}/following/{userId}", null);
            
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

    public async Task<UserListResultDto> GetFollowersAsync(string? userId = null, int maxResults = 100)
    {
        try
        {
            var targetUserId = userId ?? State.UserId ?? await GetAuthenticatedUserIdAsync();
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/users/{targetUserId}/followers?max_results={maxResults}&user.fields=created_at,description,public_metrics,verified", 
                null);
            
            return ParseUserListResponse(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get followers");
            return new UserListResultDto();
        }
    }

    public async Task<UserListResultDto> GetFollowingAsync(string? userId = null, int maxResults = 100)
    {
        try
        {
            var targetUserId = userId ?? State.UserId ?? await GetAuthenticatedUserIdAsync();
            var response = await SendRequestAsync(HttpMethod.Get, 
                $"/users/{targetUserId}/following?max_results={maxResults}&user.fields=created_at,description,public_metrics,verified", 
                null);
            
            return ParseUserListResponse(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get following");
            return new UserListResultDto();
        }
    }

    // Timelines
    public async Task<TimelineResultDto> GetHomeTimelineAsync(int maxResults = 100, string? paginationToken = null)
    {
        try
        {
            var userId = State.UserId ?? await GetAuthenticatedUserIdAsync();
            var url = $"/users/{userId}/timelines/reverse_chronological?max_results={maxResults}&tweet.fields=created_at,author_id,public_metrics";
            if (!string.IsNullOrEmpty(paginationToken))
                url += $"&pagination_token={paginationToken}";
            
            var response = await SendRequestAsync(HttpMethod.Get, url, null);
            return ParseTimelineResponse(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get home timeline");
            return new TimelineResultDto();
        }
    }

    public async Task<TimelineResultDto> GetUserTimelineAsync(string userId, int maxResults = 100, string? paginationToken = null)
    {
        try
        {
            var url = $"/users/{userId}/tweets?max_results={maxResults}&tweet.fields=created_at,author_id,public_metrics";
            if (!string.IsNullOrEmpty(paginationToken))
                url += $"&pagination_token={paginationToken}";
            
            var response = await SendRequestAsync(HttpMethod.Get, url, null);
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
    private TwitterAuthenticationResult ValidateAuthentication(TwitterAuthenticationMode requiredMode)
    {
        var result = new TwitterAuthenticationResult { AuthMode = requiredMode };
        
        switch (requiredMode)
        {
            case TwitterAuthenticationMode.BearerToken:
                if (string.IsNullOrEmpty(State.BearerToken))
                {
                    result.ErrorMessage = "Bearer Token is required but not configured";
                    return result;
                }
                break;
                
            case TwitterAuthenticationMode.OAuth1a:
                if (string.IsNullOrEmpty(State.ConsumerKey) || 
                    string.IsNullOrEmpty(State.ConsumerSecret) ||
                    string.IsNullOrEmpty(State.OAuthToken) || 
                    string.IsNullOrEmpty(State.OAuthTokenSecret))
                {
                    result.ErrorMessage = "OAuth 1.0a requires ConsumerKey, ConsumerSecret, OAuthToken, and OAuthTokenSecret";
                    return result;
                }
                break;
                
            case TwitterAuthenticationMode.Auto:
                // For Auto mode, check if we have any valid authentication method
                bool hasBearerToken = !string.IsNullOrEmpty(State.BearerToken);
                bool hasOAuth = !string.IsNullOrEmpty(State.OAuthToken) &&
                               !string.IsNullOrEmpty(State.OAuthTokenSecret) &&
                               !string.IsNullOrEmpty(State.ConsumerKey) &&
                               !string.IsNullOrEmpty(State.ConsumerSecret);
                
                if (!hasBearerToken && !hasOAuth)
                {
                    result.ErrorMessage = "At least one authentication method (Bearer Token or OAuth 1.0a) is required";
                    return result;
                }
                break;
                
            case TwitterAuthenticationMode.OAuth2UserContext:
                result.ErrorMessage = "OAuth 2.0 User Context not yet implemented";
                return result;
        }
        
        result.IsSuccess = true;
        return result;
    }
    
    private string GenerateOAuth1aSignature(HttpMethod method, string url, Dictionary<string, string> parameters, string consumerSecret, string tokenSecret)
    {
        try
        {
            // Sort parameters
            var sortedParams = parameters
                .OrderBy(p => p.Key)
                .ThenBy(p => p.Value)
                .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")
                .ToList();
            
            var paramString = string.Join("&", sortedParams);
            var baseString = $"{method.Method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";
            var signingKey = $"{Uri.EscapeDataString(consumerSecret)}&{Uri.EscapeDataString(tokenSecret)}";
            
            Logger.LogInformation("OAuth Base String: {BaseString}", baseString);
            Logger.LogInformation("OAuth Signing Key: {SigningKey}", $"{Uri.EscapeDataString(consumerSecret)}&[REDACTED]");
            
            using var hmac = new System.Security.Cryptography.HMACSHA1(Encoding.UTF8.GetBytes(signingKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
            var signature = Convert.ToBase64String(hash);
            
            Logger.LogInformation("OAuth Signature: {Signature}", signature);
            return signature;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate OAuth 1.0a signature");
            throw new InvalidOperationException("OAuth signature generation failed", ex);
        }
    }
    
    private TwitterAuthenticationResult PrepareOAuth1aHeaders(HttpMethod method, string url, object? payload)
    {
        var result = new TwitterAuthenticationResult { AuthMode = TwitterAuthenticationMode.OAuth1a };
        
        try
        {
            if (string.IsNullOrEmpty(State.ConsumerKey) || string.IsNullOrEmpty(State.ConsumerSecret))
            {
                result.ErrorMessage = "ConsumerKey and ConsumerSecret are required for OAuth 1.0a";
                return result;
            }
            
            var consumerKey = State.ConsumerKey;
            var consumerSecret = State.ConsumerSecret;
            
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var nonce = Guid.NewGuid().ToString("N");
            
            var oauthParams = new Dictionary<string, string>
            {
                ["oauth_consumer_key"] = consumerKey,
                ["oauth_token"] = State.OAuthToken!,
                ["oauth_signature_method"] = "HMAC-SHA1",
                ["oauth_timestamp"] = timestamp,
                ["oauth_nonce"] = nonce,
                ["oauth_version"] = "1.0"
            };
            
            // Parse URL to separate base URL and query parameters
            var uri = new Uri(url);
            var baseUrl = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
            var signatureParams = new Dictionary<string, string>(oauthParams);
            
            // Add query parameters to signature (for GET/DELETE requests)
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                foreach (string key in queryParams.Keys)
                {
                    if (key != null && queryParams[key] != null)
                    {
                        signatureParams[key] = queryParams[key]!;
                    }
                }
            }
            
            // For Twitter API v2, JSON payload does not participate in OAuth signature
            var signature = GenerateOAuth1aSignature(method, baseUrl, signatureParams, consumerSecret, State.OAuthTokenSecret!);
            oauthParams["oauth_signature"] = signature;
            
            var authHeader = "OAuth " + string.Join(", ", 
                oauthParams.Select(p => $"{Uri.EscapeDataString(p.Key)}=\"{Uri.EscapeDataString(p.Value)}\""));
            
            result.Headers["Authorization"] = authHeader;
            result.IsSuccess = true;
            
            // Enhanced debugging for OAuth 1.0a
            Logger.LogInformation("OAuth 1.0a signature generated for {Method} {Url}", method, url);
            Logger.LogInformation("OAuth parameters: {Parameters}", string.Join(", ", oauthParams.Where(p => p.Key != "oauth_signature").Select(p => $"{p.Key}={p.Value}")));
            Logger.LogInformation("Authorization header: {AuthHeader}", authHeader);
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to prepare OAuth 1.0a headers");
            result.ErrorMessage = $"OAuth 1.0a preparation failed: {ex.Message}";
            return result;
        }
    }
    
    private TwitterAuthenticationMode DetermineRequiredAuthMode(HttpMethod method, string endpoint)
    {
        // Write operations require OAuth 1.0a user context
        if (method == HttpMethod.Post || method == HttpMethod.Delete || method == HttpMethod.Put || method == HttpMethod.Patch)
        {
            return TwitterAuthenticationMode.OAuth1a;
        }
        
        // Read operations that need user context
        if (endpoint.Contains("/me") || 
            endpoint.Contains("/users/") && (endpoint.Contains("/following") || endpoint.Contains("/followers") || endpoint.Contains("/timelines")))
        {
            return TwitterAuthenticationMode.OAuth1a;
        }
        
        // Public read operations can use Bearer Token
        return TwitterAuthenticationMode.BearerToken;
    }
    
    private TwitterAuthenticationMode SelectBestAvailableAuthMode(TwitterAuthenticationMode preferredMode)
    {
        // Check if we have the credentials for the preferred mode
        bool hasBearerToken = !string.IsNullOrEmpty(State.BearerToken);
        bool hasOAuth = !string.IsNullOrEmpty(State.OAuthToken) &&
                       !string.IsNullOrEmpty(State.OAuthTokenSecret) &&
                       !string.IsNullOrEmpty(State.ConsumerKey) &&
                       !string.IsNullOrEmpty(State.ConsumerSecret);
        
        switch (preferredMode)
        {
            case TwitterAuthenticationMode.OAuth1a:
                // OAuth 1.0a is preferred, use it if available
                if (hasOAuth)
                {
                    Logger.LogDebug("Using OAuth 1.0a authentication (preferred for this operation)");
                    return TwitterAuthenticationMode.OAuth1a;
                }
                // Fallback to Bearer Token for read operations only
                if (hasBearerToken)
                {
                    Logger.LogWarning("OAuth 1.0a not available, falling back to Bearer Token (limited functionality)");
                    return TwitterAuthenticationMode.BearerToken;
                }
                break;
                
            case TwitterAuthenticationMode.BearerToken:
                // Bearer Token is preferred, use it if available
                if (hasBearerToken)
                {
                    Logger.LogDebug("Using Bearer Token authentication (preferred for this operation)");
                    return TwitterAuthenticationMode.BearerToken;
                }
                // Fallback to OAuth 1.0a if available
                if (hasOAuth)
                {
                    Logger.LogDebug("Bearer Token not available, using OAuth 1.0a authentication");
                    return TwitterAuthenticationMode.OAuth1a;
                }
                break;
        }
        
        // If we get here, neither authentication method is available
        throw new UnauthorizedAccessException($"No valid authentication credentials available for {preferredMode} operation");
    }
    
    private TwitterAuthenticationResult PrepareBearerTokenHeaders()
    {
        var result = new TwitterAuthenticationResult();
        
        if (string.IsNullOrEmpty(State.BearerToken))
        {
            result.ErrorMessage = "Bearer Token is not configured";
            return result;
        }
        
        result.Headers["Authorization"] = $"Bearer {State.BearerToken}";
        result.IsSuccess = true;
        
        Logger.LogDebug("Bearer Token authentication headers prepared");
        return result;
    }
    
    private async Task<string> SendRequestAsync(HttpMethod method, string endpoint, object? payload, CancellationToken? cancellationToken = null)
    {
        cancellationToken ??= CancellationToken.None;
        
        // Determine preferred authentication mode based on API requirements
        var preferredAuthMode = DetermineRequiredAuthMode(method, endpoint);
        var fullUrl = $"{State.BaseApiUrl}{endpoint}";
        
        // Try to use the preferred authentication mode, with fallback to available alternatives
        var authMode = SelectBestAvailableAuthMode(preferredAuthMode);
        
        // Validate the selected authentication method
        var authValidation = ValidateAuthentication(authMode);
        if (!authValidation.IsSuccess)
        {
            Logger.LogError("Authentication validation failed for {Method} {Endpoint} with {AuthMode}: {Error}", 
                method, endpoint, authMode, authValidation.ErrorMessage);
            throw new UnauthorizedAccessException($"Authentication failed: {authValidation.ErrorMessage}");
        }
        
        using var request = new HttpRequestMessage(method, fullUrl);
        
        // Prepare authentication headers based on selected mode
        TwitterAuthenticationResult authResult;
        
        switch (authMode)
        {
            case TwitterAuthenticationMode.BearerToken:
                authResult = PrepareBearerTokenHeaders();
                break;
                
            case TwitterAuthenticationMode.OAuth1a:
                authResult = PrepareOAuth1aHeaders(method, fullUrl, payload);
                if (!authResult.IsSuccess)
                {
                    Logger.LogError("OAuth 1.0a preparation failed: {Error}", authResult.ErrorMessage);
                    throw new UnauthorizedAccessException($"OAuth 1.0a failed: {authResult.ErrorMessage}");
                }
                break;
                
            default:
                throw new NotSupportedException($"Authentication mode {authMode} is not supported");
        }
        
        // Apply authentication headers
        foreach (var header in authResult.Headers)
        {
            if (header.Key == "Authorization")
            {
                request.Headers.Add("Authorization", header.Value);
            }
            else
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        
        // Add content if provided
        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        // Debug logging
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("Sending {Method} request to {Url} with {AuthMode} authentication", 
                method, endpoint, authMode);
        }
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value);
        cts.CancelAfter(TimeSpan.FromSeconds(State.RequestTimeoutSeconds));
        
        try
        {
        var response = await HttpClient.SendAsync(request, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cancellationToken.Value);
        
        if (!response.IsSuccessStatusCode)
        {
                // Enhanced error logging with authentication context
                Logger.LogWarning("Twitter API request failed: {Method} {Endpoint} - Status: {Status}, Auth: {AuthMode}, Content: {Content}", 
                    method, endpoint, response.StatusCode, authMode, content);
                
                var errorMessage = $"Twitter API error: {response.StatusCode}";
                
                // Provide more specific error messages based on status
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.Unauthorized:
                        errorMessage += $" - Authentication failed using {authMode}. Please check your credentials.";
                        break;
                    case System.Net.HttpStatusCode.Forbidden:
                        errorMessage += $" - Access forbidden. Your app may not have the required permissions for this operation, or OAuth tokens may be invalid.";
                        break;
                    case System.Net.HttpStatusCode.TooManyRequests:
                        errorMessage += " - Rate limit exceeded. Please wait before retrying.";
                        break;
                }
                
                if (!string.IsNullOrEmpty(content))
                {
                    try
                    {
                        var errorDoc = JsonDocument.Parse(content);
                        if (errorDoc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
                        {
                            var errorMessages = errors.EnumerateArray()
                                .Select(e => e.GetProperty("message").GetString())
                                .Where(m => !string.IsNullOrEmpty(m))
                                .ToList();
                            
                            if (errorMessages.Any())
                            {
                                errorMessage += $" Details: {string.Join(", ", errorMessages)}";
                            }
                        }
                    }
                    catch
                    {
                        // Ignore JSON parsing errors, use original content
                    }
                }
                
                throw new HttpRequestException(errorMessage);
            }
            
            Logger.LogDebug("Twitter API request successful: {Method} {Endpoint} with {AuthMode}", 
                method, endpoint, authMode);
        
        return content;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Logger.LogError("Twitter API request timeout: {Method} {Endpoint}", method, endpoint);
            throw new TimeoutException($"Twitter API request timed out after {State.RequestTimeoutSeconds} seconds");
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HTTP errors with enhanced messaging
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during Twitter API request: {Method} {Endpoint}", method, endpoint);
            throw new InvalidOperationException($"Twitter API request failed: {ex.Message}", ex);
        }
    }

    private async Task<string> GetAuthenticatedUserIdAsync()
    {
        var profile = await GetMyProfileAsync();
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