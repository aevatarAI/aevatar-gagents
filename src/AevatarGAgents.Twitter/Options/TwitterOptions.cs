using System.Collections.Generic;

namespace AevatarGAgents.Twitter.Options;

public class TwitterOptions
{
    public string ConsumerKey { get; set; }
    public string ConsumerSecret { get; set; }
    public string EncryptionPassword { get; set; }
    public string BearerToken { get; set; }
    public int ReplyLimit { get; set; }
}