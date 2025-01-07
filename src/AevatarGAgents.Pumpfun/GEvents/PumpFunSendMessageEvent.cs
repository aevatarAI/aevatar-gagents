using Aevatar.Core.Abstractions;
using Json.Schema.Generation;
using Orleans;

namespace AevatarGAgents.PumpFun.EventDtos;

[Description("Send a mesage to PumpFunAgent.")]
[GenerateSerializer]
public class PumpFunSendMessageEvent : EventBase
{
    [Description("ReplyId is from PumpFunSendMessageGEvent")]
    [Id(1)]
    public string? ReplyId { get; set; }

    [Description("Text content of the ReplyMessage to be sent.")]
    [Id(2)]
    public string? ReplyMessage { get; set; }
}