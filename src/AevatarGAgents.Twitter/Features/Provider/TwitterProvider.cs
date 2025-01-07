using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AevatarGAgents.Twitter.Common;
using AevatarGAgents.Twitter.Dto;
using AevatarGAgents.Twitter.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AevatarGAgents.Twitter.Provider;

public interface ITwitterProvider
{
    public Task PostTwitterAsync(string message, string accessToken, string accessTokenSecret);
    public Task ReplyAsync(string message, string tweetId, string accessToken, string accessTokenSecret);
    public Task<List<Tweet>> GetMentionsAsync(string userName);
    public Task<string> GetUserName(string accessToken, string accessTokenSecret);
    public Task LikeAsync(string tweetId, string userId, string accessToken, string accessTokenSecret);
    public Task QuoteTweetAsync(string tweetId, string message, string accessToken, string accessTokenSecret);
    public Task RetweetAsync(string tweetId, string message, string accessToken, string accessTokenSecret);
}


public class TwitterProvider : ITwitterProvider, ISingletonDependency
{
    private readonly ILogger<ITwitterProvider> _logger;
    private readonly IOptionsMonitor<TwitterOptions> _twitterOptions;
    private readonly HttpClient _httpClient;
    private readonly AESCipher _aesCipher;
    
    public TwitterProvider(ILogger<ITwitterProvider> logger, IOptionsMonitor<TwitterOptions> twitterOptions)
    {
        _logger = logger;
        _twitterOptions = twitterOptions;
        _httpClient = new HttpClient();
        string password = twitterOptions.CurrentValue.EncryptionPassword;
        _aesCipher = new AESCipher(password);
    }
    
    public async Task<List<Tweet>> GetMentionsAsync(string userName)
    {
        var bearerToken = _twitterOptions.CurrentValue.BearerToken;
        string query = $"@{userName}";
        string encodedQuery = Uri.EscapeDataString(query);
        string url = $"https://api.twitter.com/2/tweets/search/recent?query={encodedQuery}&tweet.fields=author_id,conversation_id&max_results=100";
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("GetMentionsAsync Response: {resp}", responseBody);
            var responseData = JsonConvert.DeserializeObject<TwitterResponseDto>(responseBody);
            if (responseData?.Data != null)
            {
                return responseData.Data;
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("GetMentionsAsync Error: {err}, code: {code}", e.Message, e.StatusCode);
        }
        
        return new List<Tweet>();
    }
    
    private string GetDecryptedData(string data)
    {
        try
        {
            return  _aesCipher.Decrypt(data);
        }
        catch (Exception e)
        {
            _logger.LogError("GetDecryptedData Error: {err}, data: {data}", e.Message, data);
        }
        return "";
    }
    
    public async Task PostTwitterAsync(string message, string accessToken, string accessTokenSecret)
    {
        var url = "https://api.twitter.com/2/tweets";
        _logger.LogInformation("PostTwitterAsync message: {msg}", message);

        accessToken = GetDecryptedData(accessToken);
        accessTokenSecret = GetDecryptedData(accessTokenSecret);
        string authHeader = GenerateOAuthHeader("POST", url, accessToken, accessTokenSecret);

        var jsonBody = JsonConvert.SerializeObject(new { text = message });

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("PostTwitterAsync Response: {resp}", responseData);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("PostTwitterAsync Error: {err}, code: {code}", e.Message, e.StatusCode);
        }
    }
    
    public async Task<string> GetUserName(string accessToken, string accessTokenSecret)
    {
        var url = "https://api.x.com/2/users/me";

        accessToken = GetDecryptedData(accessToken);
        accessTokenSecret = GetDecryptedData(accessTokenSecret);
        string authHeader = GenerateOAuthHeader("GET", url, accessToken, accessTokenSecret);

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("GetUserName Response: {res[}", responseBody);
            var responseData = JsonConvert.DeserializeObject<UserInfoDto>(responseBody);
            if (responseData?.UserName != null)
            {
                return responseData.UserName;
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("Request GetUserName Error, err: {err}", e.Message);
        }

        return "";
    }
    
    public async Task ReplyAsync(string message, string tweetId, string accessToken, string accessTokenSecret)
    {
        var url = "https://api.twitter.com/2/tweets";
        
        accessToken = GetDecryptedData(accessToken);
        accessTokenSecret = GetDecryptedData(accessTokenSecret);
        string authHeader = GenerateOAuthHeader("POST", url, accessToken, accessTokenSecret);
        
        var jsonBody = JsonConvert.SerializeObject(new
        {
            text = message,
            reply = new
            {
                in_reply_to_tweet_id = tweetId
            }
        });
        
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        
        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseData);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("ReplyAsync Error: {err}, code: {code}", e.Message, e.StatusCode);
        }
    }
    
    private string GenerateOAuthHeader(string httpMethod, string url, string accessToken, string accessTokenSecret, Dictionary<string, string>? additionalParams = null)
    {
        var consumerKey = _twitterOptions.CurrentValue.ConsumerKey;
        var consumerSecret = _twitterOptions.CurrentValue.ConsumerSecret;
      
        var oauthParameters = new Dictionary<string, string>
        {
            { "oauth_consumer_key", consumerKey },
            { "oauth_nonce", Guid.NewGuid().ToString("N") },
            { "oauth_signature_method", "HMAC-SHA1" },
            { "oauth_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
            { "oauth_token", accessToken },
            { "oauth_version", "1.0" }
        };
        
        var allParams = new Dictionary<string, string>(oauthParameters);
        if (additionalParams != null)
        {
            foreach (var param in additionalParams)
            {
                allParams.Add(param.Key, param.Value);
            }
        }
        
        var sortedParams = allParams.OrderBy(kvp => kvp.Key).ThenBy(kvp => kvp.Value);
        var parameterString = string.Join("&", sortedParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var signatureBaseString = $"{httpMethod.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(parameterString)}";
        
        var signingKey = $"{Uri.EscapeDataString(consumerSecret)}&{Uri.EscapeDataString(accessTokenSecret)}";
        
        string oauthSignature;
        using (var hasher = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey)))
        {
            var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(signatureBaseString));
            oauthSignature = Convert.ToBase64String(hash);
        }
        
        allParams.Add("oauth_signature", oauthSignature);

        var authHeader = "OAuth " + string.Join(", ",
            allParams.OrderBy(kvp => kvp.Key)
                    .Where(kvp => kvp.Key.StartsWith("oauth_"))
                    .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}=\"{Uri.EscapeDataString(kvp.Value)}\""));

        return authHeader;
    }
    
    public async Task LikeAsync(string tweetId, string userId, string accessToken, string accessTokenSecret)
    {
        var url = $"https://api.twitter.com/2/users/{userId}/likes";
        _logger.LogInformation("LikeAsync tweetId: {tweetId}, userId: {userId}", tweetId, userId);

        accessToken = GetDecryptedData(accessToken);
        accessTokenSecret = GetDecryptedData(accessTokenSecret);
        string authHeader = GenerateOAuthHeader("POST", url, accessToken, accessTokenSecret);

        var jsonBody = JsonConvert.SerializeObject(new { tweet_id = tweetId });

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("LikeAsync Response: {resp}", responseData);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("LikeAsync Error: {err}, code: {code}", e.Message, e.StatusCode);
        }
    }
    
    public async Task QuoteTweetAsync(string tweetId, string message, string accessToken, string accessTokenSecret)
    {
        var url = "https://api.twitter.com/2/tweets";
        _logger.LogInformation("QuoteTweetAsync message: {msg}, quote_tweet: {tweetId}", message, tweetId);

        accessToken = GetDecryptedData(accessToken);
        accessTokenSecret = GetDecryptedData(accessTokenSecret);
        string authHeader = GenerateOAuthHeader("POST", url, accessToken, accessTokenSecret);

        var jsonBody = JsonConvert.SerializeObject(
            new { text = message, quote_tweet_id = tweetId});

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("QuoteTweetAsync Response: {resp}", responseData);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("QuoteTweetAsync Error: {err}, code: {code}", e.Message, e.StatusCode);
        }
    }
    
    public async Task RetweetAsync(string tweetId, string userId, string accessToken, string accessTokenSecret)
    {
        var url = $"https://api.twitter.com/2/users/{userId}/retweets";
        _logger.LogInformation("RetweetAsync tweetId: {tweetId}, userId: {userId}", tweetId, userId);

        accessToken = GetDecryptedData(accessToken);
        accessTokenSecret = GetDecryptedData(accessTokenSecret);
        string authHeader = GenerateOAuthHeader("POST", url, accessToken, accessTokenSecret);

        var jsonBody = JsonConvert.SerializeObject(new { tweet_id = tweetId });

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("RetweetAsync Response: {resp}", responseData);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("RetweetAsync Error: {err}, code: {code}", e.Message, e.StatusCode);
        }
    }
}

