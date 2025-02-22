using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Telegram.Options;

[GenerateSerializer]
public class TelegramOptionsDto : ConfigurationBase
{
    [Id(0)] public string Webhook { get; set; }
    [Id(1)] public string EncryptionPassword { get; set; }
}