using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Orleans;

namespace Aevatar.GAgents.Twitter.Options;

[GenerateSerializer]
public class InitTwitterOptionsDto : ConfigurationBase
{
    [Id(0)]
    [Required(ErrorMessage = "Consumer Key is required")]
    [StringLength(50, MinimumLength = 10, ErrorMessage = "Consumer Key must be between 10 and 50 characters")]
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Consumer Key can only contain alphanumeric characters")]
    public string ConsumerKey { get; set; } = "YOUR_TWITTER_API_KEY";
    
    [Id(1)]
    [Required(ErrorMessage = "Consumer Secret is required")]
    [StringLength(100, MinimumLength = 20, ErrorMessage = "Consumer Secret must be between 20 and 100 characters")]
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Consumer Secret can only contain alphanumeric characters")]
    public string ConsumerSecret { get; set; } = "YOUR_API_SECRET";
    
    [Id(2)]
    [Required(ErrorMessage = "Encryption Password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Encryption Password must be between 8 and 128 characters")]
    public string EncryptionPassword { get; set; } = "YOUR_ENCRYPTION_PASSWORD";
    
    [Id(3)]
    [Required(ErrorMessage = "Bearer Token is required")]
    [StringLength(200, MinimumLength = 30, ErrorMessage = "Bearer Token must be between 30 and 200 characters")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Bearer Token format is invalid")]
    public string BearerToken { get; set; } = "YOUR_BEARER_TOKEN";
    
    [Id(4)]
    [Range(1, 100, ErrorMessage = "Reply Limit must be between 1 and 100")]
    public int ReplyLimit { get; set; } = 10;
}