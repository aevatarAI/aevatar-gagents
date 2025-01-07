using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.PumpFun.Agent.GEvents;
[GenerateSerializer]
public class PumpFunSendMessageGEvent : GEventBase
{
    [Id(1)] public string? ChatId { get; set; }
    [Id(2)] public string? ReplyId { get; set; }
    [Id(3)] public string? ReplyMessage { get; set; } 
}