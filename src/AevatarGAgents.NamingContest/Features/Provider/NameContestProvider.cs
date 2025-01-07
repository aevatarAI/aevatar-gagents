using System;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AevatarGAgents.NamingContest.Common;
using AevatarGAgents.NamingContest.VoteGAgent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AevatarGAgents.NamingContest.Provider;

public class NameContestProvider : INameContestProvider,ISingletonDependency
{
    private readonly ILogger<NameContestProvider> _logger;



    public NameContestProvider(ILogger<NameContestProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task SendMessageAsync(Guid groupId,NamingAILogEvent? namingLogEvent,string callBackUrl)
    {
        
        // Serialize the request object to JSON
        var json = JsonConvert.SerializeObject(namingLogEvent, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        
        

        try
        {
            JsonNode? jsonObject = JsonNode.Parse(json);
            jsonObject!["GroupId"] = groupId;
            var updatedJson = jsonObject.ToString();
            
            _logger.LogDebug("NameContestProvider send message  {replyMessage} to  {addresss}",updatedJson, callBackUrl);
            var client = new HttpClient();
            client.BaseAddress = new Uri(callBackUrl);
            
            var response = await client.PostAsync(callBackUrl, new StringContent(updatedJson, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            if (namingLogEvent != null)
            {
                _logger.LogDebug("NameContestProvider send message end  {replyId} : {response}", namingLogEvent.EventId,
                    response);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseBody);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"request NameContest callback url error: {e.Message}");
        }
    }

    public async Task SendMessageAsync(Guid groupId, VoteCharmingCompleteEvent? voteCharmingCompleteEvent, string callBackUrl)
    {
        // Serialize the request object to JSON
        var json = JsonConvert.SerializeObject(voteCharmingCompleteEvent, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        
        

        try
        {
            JsonNode? jsonObject = JsonNode.Parse(json);
            jsonObject!["GroupId"] = groupId;
            var updatedJson = jsonObject.ToString();
            
            _logger.LogDebug("NameContestProvider send VoteCharmingCompleteEvent  {replyMessage} to  {addresss}",updatedJson, callBackUrl);
            var client = new HttpClient();
            client.BaseAddress = new Uri(callBackUrl);
            
            var response = await client.PostAsync(callBackUrl, new StringContent(updatedJson, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            if (voteCharmingCompleteEvent != null)
            {
                _logger.LogDebug("NameContestProvider send VoteCharmingCompleteEvent end  {replyId} : {response}", voteCharmingCompleteEvent,
                    response);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseBody);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"request NameContest VoteCharmingCompleteEvent  callback url error: {e.Message}");
        }
    }
}