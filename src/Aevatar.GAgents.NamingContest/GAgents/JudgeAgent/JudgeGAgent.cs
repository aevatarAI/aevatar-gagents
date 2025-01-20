
using System.Text.Json;
using System.Text.Json.Serialization;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgent.NamingContest.Common;
using Aevatar.GAgent.NamingContest.TrafficGAgent;
using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;
using Aevatar.GAgents.NamingContest.Common;
using AiSmart.GAgent.NamingContest.JudgeAgent.Dto;
using AiSmart.GAgent.NamingContest.VoteAgent;
using Microsoft.Extensions.Logging;

namespace AiSmart.GAgent.NamingContest.JudgeAgent;

[GAgent(nameof(JudgeGAgent))]
public class JudgeGAgent : GAgentBase<JudgeState, JudgeCloneStateLogEvent,EventBase, InitJudgeDto>, IJudgeGAgent
{
    public JudgeGAgent(ILogger<JudgeGAgent> logger) : base(logger)
    {
    }

    [EventHandler]
    public async Task HandleEventAsync(TrafficNamingContestOver @event)
    {
        await PublishAsync(new JudgeOverGEvent() { NamingQuestion = @event.NamingQuestion });
        RaiseEvent(new JudgeClearAIStateLogEvent());
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(JudgeVoteGEVent @event)
    {
        if (@event.JudgeGrainId != this.GetPrimaryKey())
        {
            return;
        }

        Logger.LogInformation($"[JudgeGAgent] JudgeVoteGEVent Start GrainId:{this.GetPrimaryKey().ToString()}");
        var judgeResponse = new JudgeVoteChatResponse();
        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(NamingConstants.JudgeVotePrompt, @event.History);
            if (response != null && !response.Content.IsNullOrEmpty())
            {
                var voteResult = JsonSerializer.Deserialize<JudgeVoteChatResponse>(response.Content);
                if (voteResult != null)
                {
                    judgeResponse = voteResult;
                }
                else
                {
                    Logger.LogError($"[Judge] response voteResult == null response content:{response.Content}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Judge] JudgeVoteGEVent error");
        }
        finally
        {
            if (judgeResponse.Name.IsNullOrWhiteSpace())
            {
                Logger.LogError("[Judge] JudgeVoteGEVent Vote name is empty");
            }

            await PublishAsync(new JudgeVoteResultGEvent()
            {
                VoteName = judgeResponse.Name, Reason = judgeResponse.Reason, JudgeGrainId = this.GetPrimaryKey(),
                RealJudgeGrainId = GetRealJudgeId(),
                JudgeName = State.AgentName
            });

            // Logger.LogInformation($"[JudgeGAgent] JudgeVoteGEVent End GrainId:{this.GetPrimaryKey().ToString()}");
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
                await PublishAsync(new NamingAiLogGEvent(NamingContestStepEnum.JudgeAsking, GetRealJudgeId(),
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
                await PublishAsync(new NamingAiLogGEvent(NamingContestStepEnum.JudgeScore, GetRealJudgeId(),
                    NamingRoleType.Judge, State.AgentName, defaultScore, prompt));
            }

            await PublishAsync(new JudgeScoreCompleteGEvent() { JudgeGrainId = this.GetPrimaryKey() });
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(SingleVoteCharmingGEvent gEvent)
    {
        var agentNames = string.Join(" and ", gEvent.AgentIdNameDictionary.Values);
        var prompt = NamingConstants.VotePrompt.Replace("$AgentNames$", agentNames);
        try
        {
            var message = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, gEvent.VoteMessage);

            if (message != null && !message.Content.IsNullOrEmpty())
            {
                var namingReply = message.Content.Replace("\"", "").ToLower();
                var agent = gEvent.AgentIdNameDictionary.FirstOrDefault(x => x.Value.ToLower().Equals(namingReply));
                var winner = agent.Key;
                await PublishAsync(new VoteCharmingCompleteEvent()
                {
                    Winner = winner,
                    VoterId = GetRealJudgeId(),
                    Round = gEvent.Round
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[JudgeGAgent] SingleVoteCharmingEvent ");
        }
    }

    private Guid GetRealJudgeId()
    {
        return State.CloneJudgeId == Guid.Empty ? this.GetPrimaryKey() : State.CloneJudgeId;
    }

    public async Task SetRealJudgeGrainId(Guid judgeGrainId)
    {
        RaiseEvent(new JudgeCloneStateLogEvent() { JudgeGrainId = judgeGrainId });
        await ConfirmEvents();
    }

    public Task<MicroAIGAgentState> GetStateAsync()
    {
        throw new NotImplementedException();
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("the judge agent");
    }

    public async Task SetAgent(string agentName, string agentResponsibility)
    {
        RaiseEvent(new AISetAgentStateLogEvent
        {
            AgentName = agentName,
            AgentResponsibility = agentResponsibility
        });
        await ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(agentName).SetAgentAsync(agentResponsibility);
    }

    public async Task SetAgentWithTemperatureAsync(string agentName, string agentResponsibility, float temperature,
        int? seed = null,
        int? maxTokens = null)
    {
        RaiseEvent(new AISetAgentStateLogEvent
        {
            AgentName = agentName,
            AgentResponsibility = agentResponsibility
        });
        await ConfirmEvents();
        await GrainFactory.GetGrain<IChatAgentGrain>(agentName)
            .SetAgentWithTemperature(agentResponsibility, temperature, seed, maxTokens);
    }

    public Task<MicroAIGAgentState> GetAgentState()
    {
        throw new NotImplementedException();
    }

    public async override Task InitializeAsync(InitJudgeDto initializeDto)
    {
        RaiseEvent(new AISetAgentStateLogEvent
        {
            AgentName = initializeDto.AgentName,
            AgentResponsibility = initializeDto.AgentResponsibility,
            CloneJudge = initializeDto.CloneJudgeId
        });
        await ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(initializeDto.AgentName).SetAgentAsync(initializeDto.AgentResponsibility);
    }
}

public class JudgeVoteChatResponse
{
    [JsonPropertyName(@"name")] public string Name { get; set; } = "";

    [JsonPropertyName(@"reason")] public string Reason { get; set; } = "";
}