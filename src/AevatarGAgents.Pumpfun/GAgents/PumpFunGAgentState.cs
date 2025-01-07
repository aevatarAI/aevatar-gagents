using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using AevatarGAgents.PumpFun.Agent.GEvents;
using Orleans;

namespace AevatarGAgents.PumpFun.Agent;
[GenerateSerializer]
public class PumpFunGAgentState : StateBase
{
    [Id(0)] public Dictionary<string, PumpFunReceiveMessageGEvent> requestMessage { get; set; } = new Dictionary<string, PumpFunReceiveMessageGEvent>();
    [Id(1)] public Dictionary<string, PumpFunSendMessageGEvent> responseMessage { get; set; } = new Dictionary<string, PumpFunSendMessageGEvent>();
    
    public void Apply(PumpFunReceiveMessageGEvent receiveMessageGEvent)
    {
        requestMessage[receiveMessageGEvent.ReplyId] = receiveMessageGEvent;
    }
    
    public void Apply(PumpFunSendMessageGEvent sendMessageGEvent)
    {
        responseMessage[sendMessageGEvent.ReplyId] = sendMessageGEvent;
        requestMessage.Remove(sendMessageGEvent.ReplyId);
    }

}