using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Orleans;

namespace Aevatar.GAgents.Telegram.Options;

[GenerateSerializer]
public class TelegramOptionsDto : ConfigurationBase
{
    [Id(0)]
    [DefaultValues("https://your-domain.com/webhook")]
    public string Webhook { get; set; }
    
    [Id(1)]
    [DefaultValues("YOUR_ENCRYPTION_PASSWORD")]
    public string EncryptionPassword { get; set; }
    
    [Id(2)]
    [DefaultValues("YOUR_BOT_TOKEN")]
    public string BotToken { get; set; }
    
    [Id(3)]
    [DefaultValues(100, 50, 200)]
    public int MaxConnections { get; set; } = 100;
}