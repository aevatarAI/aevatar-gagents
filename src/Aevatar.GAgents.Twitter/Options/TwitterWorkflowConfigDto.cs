using GroupChat.GAgent.Dto;
using Orleans;

namespace Aevatar.GAgents.Twitter.Options;

[GenerateSerializer]
public class TwitterWorkflowConfigDto : GroupMemberConfigDto
{
    [Id(1)] public string ConsumerKey { get; set; } = "";
    [Id(2)] public string ConsumerSecret { get; set; } = "";
    [Id(3)] public string EncryptionPassword { get; set; } = "";
    [Id(4)] public string BearerToken { get; set; } = "";
    [Id(5)] public int ReplyLimit { get; set; } = 10;
    
    // Twitter account information - no need for separate binding
    [Id(6)] public string TwitterUserId { get; set; } = "";
    [Id(7)] public string TwitterUserName { get; set; } = "";
    [Id(8)] public string TwitterToken { get; set; } = "";
    [Id(9)] public string TwitterTokenSecret { get; set; } = "";
} 