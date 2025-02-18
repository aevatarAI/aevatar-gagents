using Aevatar.Core.Abstractions;
using Orleans;

namespace AISmart.GAgent.Telegram.Options;

[GenerateSerializer]
public class TelegramOptionsDto : InitializationEventBase
{
    [Id(0)] public string Webhook { get; set; }
    [Id(1)] public string EncryptionPassword { get; set; }
}