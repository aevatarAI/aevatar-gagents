using Orleans;

namespace AISmart.GAgent.Telegram.Agent.GEvents;

[GenerateSerializer]
public class SetTelegramConfigEvent:MessageSEvent
{
    [Id(0)] public string BotName { get; set; }
    [Id(1)] public string Token { get; set; }
}