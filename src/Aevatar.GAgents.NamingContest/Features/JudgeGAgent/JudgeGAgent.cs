using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent;
using Aevatar.GAgents.MicroAI.Agent.GEvents;
using Aevatar.GAgents.MicroAI.Grains;
using Aevatar.GAgents.NamingContest.Common;
using AutoGen.Core;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.NamingContest.JudgeGAgent;
using Aevatar.GAgents.NamingContest.TrafficGAgent;
using Aevatar.GAgents.NamingContest.VoteGAgent;
using Orleans;

namespace Aevatar.GAgents.NamingContest.JudgeAgent;

public class JudgeGAgent : MicroAIGAgent, IJudgeGAgent
{
    public JudgeGAgent(ILogger<MicroAIGAgent> logger) : base(logger)
    {
    }

    [EventHandler]
    public async Task HandleEventAsync(JudgeGEvent @event)
    {
        var history = new List<MicroAIMessage>()
        {
            new MicroAIMessage(Role.User.ToString(),
                $"The theme of this naming contest is: \"{@event.NamingQuestion}\""),
        };

        history.AddRange(State.RecentMessages);
        history.Add(new MicroAIMessage(Role.User.ToString(), @event.NamingReply));

        List<AIMessageSEvent> sEvent = new List<AIMessageSEvent>();
        sEvent.Add(new AiReceiveMessageSEvent()
            { Message = new MicroAIMessage(Role.User.ToString(), @event.NamingReply) });
        var message = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
            .SendAsync(@event.NamingReply, history);

        if (message != null && !message.Content.IsNullOrEmpty())
        {
            var namingReply = message.Content;
            var score = int.Parse(namingReply);

            sEvent.Add(new AiReceiveMessageSEvent()
                { Message = new MicroAIMessage(Role.Assistant.ToString(), namingReply) });
        }

        RaiseEvents(sEvent);
        await base.ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(TrafficNamingContestOver @event)
    {
        await PublishAsync(new JudgeOverGEvent() { NamingQuestion = @event.NamingQuestion });
        RaiseEvent(new AIClearMessageSEvent());
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(JudgeVoteGEVent @event)
    {
        if (@event.JudgeGrainId != this.GetPrimaryKey())
        {
            return;
        }

        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(NamingConstants.JudgeVotePrompt, @event.History);
            if (response != null && !response.Content.IsNullOrEmpty())
            {
                var voteResult = JsonSerializer.Deserialize<JudgeVoteChatResponse>(response.Content);
                if (voteResult == null)
                {
                    _logger.LogError("");
                    return;
                }

                await PublishAsync(new JudgeVoteResultGEvent()
                {
                    VoteName = voteResult.Name, Reason = voteResult.Reason, JudgeGrainId = this.GetPrimaryKey(),
                    JudgeName = State.AgentName
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Judge] JudgeVoteGEVent error");
            await PublishAsync(new JudgeVoteResultGEvent()
            {
                VoteName = "", Reason = "", JudgeGrainId = this.GetPrimaryKey(),
                JudgeName = State.AgentName
            });
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(JudgeAskingGEvent @event)
    {
        if (@event.JudgeGuid != this.GetPrimaryKey())
        {
            return;
        }

        var reply = string.Empty;
        var prompt = NamingConstants.JudgeAskingPrompt;
        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, @event.History);
            if (response != null && !response.Content.IsNullOrEmpty())
            {
                reply = response.Content;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[JudgeGAgent] JudgeAskingGEvent error");
        }
        finally
        {
            if (!reply.IsNullOrWhiteSpace())
            {
                await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.JudgeAsking, this.GetPrimaryKey(),
                    NamingRoleType.Judge, State.AgentName, reply, prompt));
            }

            await PublishAsync(new JudgeAskingCompleteGEvent()
            {
                JudgeGuid = this.GetPrimaryKey(),
                Reply = reply,
            });
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(JudgeScoreGEvent @event)
    {
        var defaultScore = "84.3";
        var prompt = NamingConstants.JudgeScorePrompt;
        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, @event.History);
            if (response != null && !response.Content.IsNullOrEmpty())
            {
                defaultScore = response.Content;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "[JudgeGAgent] JudgeScoreGEvent error");
        }
        finally
        {
            if (!defaultScore.IsNullOrWhiteSpace())
            {
                await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.JudgeScore, this.GetPrimaryKey(),
                    NamingRoleType.Judge, State.AgentName, defaultScore, prompt));
            }

            await PublishAsync(new JudgeScoreCompleteGEvent() { JudgeGrainId = this.GetPrimaryKey() });
        }
    }
    [EventHandler]
    public async Task HandleEventAsync(SingleVoteCharmingEvent @event)
    {
        var agentNames = string.Join(" and ", @event.AgentIdNameDictionary.Values);
        var prompt = NamingConstants.VotePrompt.Replace("$AgentNames$", agentNames);
        var message = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
            .SendAsync(prompt, @event.VoteMessage);

        if (message != null && !message.Content.IsNullOrEmpty())
        {
            var namingReply = message.Content.Replace("\"","").ToLower();
            var agent = @event.AgentIdNameDictionary.FirstOrDefault(x => x.Value.ToLower().Equals(namingReply));
            var winner = agent.Key;
            await PublishAsync(new VoteCharmingCompleteEvent()
            {
                Winner = winner,
                VoterId = this.GetPrimaryKey(),
                Round = @event.Round
            });
        }
        await base.ConfirmEvents();
    }
}
