using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.PumpFun.Agent.GEvents;

[GenerateSerializer]
public class SetPumpFunConfigEvent : GEventBase
{
    [Id(0)] public string ChatId { get; set; }
    
    [Id(1)] public string BotName { get; set; }
}