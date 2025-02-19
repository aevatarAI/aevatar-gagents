using Orleans;

namespace AISmart.GAgent.Telegram.Agent.GEvents;

[GenerateSerializer]
public class TelegramOptionSEvent:MessageSEvent
{
    [Id(0)] public string Webhook { get; set; }
    [Id(1)] public string EncryptionPassword { get; set; }
}