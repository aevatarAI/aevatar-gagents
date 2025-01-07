using System;
using System.Linq;
using System.Threading.Tasks;
using AevatarGAgents.MicroAI.Agent;
using AevatarGAgents.MicroAI.Agent.GEvents;
using AevatarGAgents.MicroAI.Grains;
using AevatarGAgents.PumpFun.Grains;
using AutoGen.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Aevatar.Core.Abstractions;
using AevatarGAgents.PumpFun.EventDtos;

namespace AevatarGAgents.PumpFun.Agent;

public class PumpFunChatGAgent : MicroAIGAgent, IPumpFunChatGrain
{
    private readonly string _defaultReply = "I don't understand what you're saying.";
    
    private readonly ILogger<PumpFunChatGAgent> _logger;

    public PumpFunChatGAgent(ILogger<PumpFunChatGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    [EventHandler]
    public async Task HandleEventAsync(PumpFunReceiveMessageEvent @event)
    {
        _logger.LogInformation("PumpFunReceiveMessageEvent:" + JsonConvert.SerializeObject(@event));
        var response = _defaultReply;
        try
        {
            if (!@event.RequestMessage.IsNullOrEmpty())
            {
                var message = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                    .SendAsync(@event.RequestMessage, State.RecentMessages.ToList());
                _logger.LogInformation("PumpFunChatGAgent HandleEventAsync2:" + JsonConvert.SerializeObject(message));
                if (message != null && !message.Content.IsNullOrWhiteSpace())
                {
                    response = message.Content;
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[PumpFunChatGAgent] PumpFunReceiveMessageEvent error");
        }
        finally
        {
            await PublishAsync(new PumpFunSendMessageEvent() { ReplyId = @event.ReplyId, ReplyMessage = response });
            if (response != _defaultReply)
            {
                RaiseEvent(new AIReceiveMessageGEvent()
                    { Message = new MicroAIMessage(Role.User.ToString(), @event.RequestMessage) });
                RaiseEvent(new AIReceiveMessageGEvent()
                    { Message = new MicroAIMessage(Role.Assistant.ToString(), response) });
            }
        }
    }
}