using Aevatar.Core.Abstractions;
using Orleans;

namespace AevatarGAgents.PumpFun.Agent.GEvents;
[GenerateSerializer]
public class PumpFunReceiveMessageGEvent : GEventBase
{
    [Id(0)]  public string? ChatId { get; set; }
    [Id(1)]  public string? ReplyId { get; set; }
    [Id(2)] public string? RequestMessage { get; set; }
}