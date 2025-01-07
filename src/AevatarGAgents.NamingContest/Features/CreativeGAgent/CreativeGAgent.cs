using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Aevatar.Core;
using AevatarGAgents.MicroAI.Agent;
using AevatarGAgents.MicroAI.Agent.GEvents;
using AevatarGAgents.MicroAI.Grains;
using AevatarGAgents.NamingContest.Common;
using AutoGen.Core;
using Microsoft.Extensions.Logging;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Aevatar.Core.Abstractions;
using AevatarGAgents.NamingContest.TrafficGAgent;
using AevatarGAgents.NamingContest.VoteGAgent;
using Orleans;
using Orleans.Runtime;

namespace AevatarGAgents.NamingContest.CreativeGAgent;

public class CreativeGAgent : GAgentBase<CreativeState, CreativeSEventBase>, ICreativeGAgent
{
    private readonly ILogger<CreativeGAgent> _logger;

    public CreativeGAgent(ILogger<CreativeGAgent> logger) : base(logger)
    {
        _logger = logger;
    }

    [EventHandler]
    public async Task HandleEventAsync(GroupChatStartGEvent @event)
    {
        if (@event.IfFirstStep == true)
        {
            RaiseEvent(new AddHistoryChatSEvent()
            {
                Message = new MicroAIMessage(Role.User.ToString(), @event.ThemeDescribe)
            });
        }
        else
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(NamingConstants.CreativeSummaryHistoryPrompt, State.RecentMessages.ToList());
            if (response != null && !response.Content.IsNullOrEmpty())
            {
                // clear history message
                RaiseEvent(new ClearHistoryChatSEvent());

                // summary the naming contest
                RaiseEvent(new AddHistoryChatSEvent()
                {
                    Message = new MicroAIMessage(Role.System.ToString(),
                        AssembleMessageUtil.AssembleSummaryBeforeStep(@event.CreativeNameings, response.Content,
                            @event.ThemeDescribe))
                });
            }
        }

        await base.ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(TrafficInformCreativeGEvent @event)
    {
        GrainId grainId= await this.GetSubscriptionAsync();
        
        
        if (@event.CreativeGrainId != this.GetPrimaryKey())
        {
            return;
        }

        var namingReply = string.Empty;
        var prompt = NamingConstants.NamingPrompt;
        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, State.RecentMessages.ToList());

            if (response != null && !response.Content.IsNullOrEmpty())
            {
                namingReply = response.Content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Creative] TrafficInformCreativeGEvent error");
            namingReply = NamingConstants.DefaultCreativeNaming;
        }
        finally
        {
            await this.PublishAsync(new NamedCompleteGEvent()
            {
                Content = @event.NamingContent,
                GrainGuid = this.GetPrimaryKey(),
                NamingReply = namingReply,
                CreativeName = State.AgentName,
            });
            
            await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.Naming, this.GetPrimaryKey(),
                NamingRoleType.Contestant, State.AgentName, namingReply,prompt));
            
            RaiseEvent(new AddHistoryChatSEvent()
            {
                Message = new MicroAIMessage(Role.User.ToString(),
                    AssembleMessageUtil.AssembleNamingContent(State.AgentName, namingReply))
            });

            RaiseEvent(new SetNamingSEvent { Naming = namingReply });

            await base.ConfirmEvents();
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(NamedCompleteGEvent @event)
    {
        if (@event.GrainGuid == this.GetPrimaryKey())
        {
            return;
        }
        
        RaiseEvent(new AddHistoryChatSEvent()
        {
            Message = new MicroAIMessage(Role.User.ToString(),
                AssembleMessageUtil.AssembleNamingContent(@event.CreativeName, @event.NamingReply))
        });

        await base.ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(DebatedCompleteGEvent @event)
    {
        if (@event.GrainGuid == this.GetPrimaryKey())
        {
            return;
        }
        
        RaiseEvent(new AddHistoryChatSEvent()
        {
            Message = new MicroAIMessage(Role.User.ToString(),
                AssembleMessageUtil.AssembleDebateContent(@event.CreativeName, @event.DebateReply))
        });

        await base.ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(TrafficInformDebateGEvent @event)
    {
        if (@event.CreativeGrainId != this.GetPrimaryKey())
        {
            return;
        }

        var debateReply = string.Empty;
        var prompt = NamingConstants.DebatePrompt;
        try
        {
            var message = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, State.RecentMessages.ToList());
            if (message != null && !message.Content.IsNullOrEmpty())
            {
                debateReply = message.Content;
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Creative] TrafficInformDebateGEvent error");
            debateReply = NamingConstants.DefaultDebateContent;
        }
        finally
        {
            await this.PublishAsync(new DebatedCompleteGEvent()
            {
                Content = @event.NamingContent,
                GrainGuid = this.GetPrimaryKey(),
                DebateReply = debateReply,
                CreativeName = State.AgentName,
            });

            RaiseEvent(new AddHistoryChatSEvent()
            {
                Message = new MicroAIMessage(Role.User.ToString(),
                    AssembleMessageUtil.AssembleDebateContent(State.AgentName, debateReply))
            });
            
            await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.Debate, this.GetPrimaryKey(),
                NamingRoleType.Contestant, State.AgentName, debateReply, prompt));
            await base.ConfirmEvents();
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(DiscussionGEvent @event)
    {
        if (@event.CreativeId != this.GetPrimaryKey())
        {
            return;
        }

        var discussionReply = string.Empty;
        var prompt = NamingConstants.CreativeDiscussionPrompt;
        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, State.RecentMessages.ToList());
            if (response != null && !response.Content.IsNullOrEmpty())
            {
                discussionReply = response.Content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Creative] DiscussionGEvent error");
        }
        finally
        {
            await this.PublishAsync(new DiscussionCompleteGEvent()
            {
                CreativeId = this.GetPrimaryKey(),
                DiscussionReply = discussionReply,
                CreativeName = State.AgentName,
            });

            if (!discussionReply.IsNullOrEmpty())
            {
                RaiseEvent(new AddHistoryChatSEvent()
                {
                    Message = new MicroAIMessage(Role.User.ToString(),
                        AssembleMessageUtil.AssembleDiscussionContent(State.AgentName, discussionReply))
                });
                
                await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.Discussion, this.GetPrimaryKey(),
                    NamingRoleType.Contestant, State.AgentName, discussionReply, prompt));
            }

            await base.ConfirmEvents();
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(DiscussionCompleteGEvent @event)
    {
        if (@event.CreativeId == this.GetPrimaryKey())
        {
            return;
        }
        
        if (@event.DiscussionReply.IsNullOrEmpty())
        {
            return;
        }

        RaiseEvent(new AddHistoryChatSEvent()
        {
            Message = new MicroAIMessage(Role.User.ToString(),
                AssembleMessageUtil.AssembleDiscussionContent(@event.CreativeName, @event.DiscussionReply))
        });

        await base.ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(CreativeSummaryGEvent @event)
    {
        if (@event.CreativeId != this.GetPrimaryKey())
        {
            return;
        }

        var summary = new CreativeGroupSummary();
        var prompt = NamingConstants.CreativeGroupSummaryPrompt;
        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, State.RecentMessages.ToList());
            if (response != null && !response.Content.IsNullOrEmpty())
            {
                summary = JsonSerializer.Deserialize<CreativeGroupSummary>(response.Content);
            }

        }
        catch (Exception ex)
        {
            Logger.LogError("[Creative] CreativeSummaryGEvent error");
        }
        finally
        {
            if (summary.Name.IsNullOrEmpty())
            {
                var random = new Random();
                var index = random.Next(0, @event.CreativeNames.Count);
                var findCreative = @event.CreativeNames[index];
                summary.Name = findCreative.Item2;
                summary.Reason = NamingConstants.CreativeGroupSummaryReason;
            }

            RaiseEvent(new AddHistoryChatSEvent()
            {
                Message = new MicroAIMessage(Role.User.ToString(),
                    AssembleMessageUtil.AssembleDiscussionSummary(summary.Name, summary.Reason))
            });

            await PublishAsync(new CreativeSummaryCompleteGEvent()
                { SummaryName = summary.Name, Reason = summary.Reason, GraindId = this.GetPrimaryKey() });
            
            await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.DiscussionSummary, this.GetPrimaryKey(),
                NamingRoleType.Contestant, State.AgentName, JsonSerializer.Serialize(summary), prompt));


            await base.ConfirmEvents();
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(CreativeSummaryCompleteGEvent @event)
    {
        if (@event.GraindId == this.GetPrimaryKey())
        {
            return;
        }
        
        RaiseEvent(new AddHistoryChatSEvent()
        {
            Message = new MicroAIMessage(Role.User.ToString(),
                AssembleMessageUtil.AssembleDiscussionSummary(@event.SummaryName, @event.Reason))
        });

        await base.ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(JudgeAskingCompleteGEvent @event)
    {
        if (@event.Reply.IsNullOrEmpty())
        {
            return;
        }

        RaiseEvent(new AddHistoryChatSEvent()
        {
            Message = new MicroAIMessage(Role.User.ToString(),
                AssembleMessageUtil.AssembleJudgeAsking(@event.JudgeName, @event.Reply))
        });
    }

    [EventHandler]
    public async Task HandleEventAsync(CreativeAnswerQuestionGEvent @event)
    {
        if (@event.CreativeId != this.GetPrimaryKey())
        {
            return;
        }
        
        var answer = string.Empty;
        var prompt = NamingConstants.CreativeAnswerQuestionPrompt;
        try
        {
            var response = await GrainFactory.GetGrain<IChatAgentGrain>(State.AgentName)
                .SendAsync(prompt, State.RecentMessages.ToList());
            if (response != null && !response.Content.IsNullOrEmpty())
            {
                answer = response.Content.ToString();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("[Creative] CreativeSummaryGEvent error");
        }
        finally
        {
            if (!answer.IsNullOrWhiteSpace())
            {
                RaiseEvent(new AddHistoryChatSEvent()
                {
                    Message = new MicroAIMessage(Role.User.ToString(),
                        AssembleMessageUtil.AssembleCreativeAnswer(State.AgentName, answer))
                });
                
                await PublishAsync(new NamingAILogEvent(NamingContestStepEnum.JudgeAsking, this.GetPrimaryKey(),
                    NamingRoleType.Contestant, State.AgentName, answer,prompt ));
            }

            await PublishAsync(new CreativeAnswerCompleteGEvent()
                { CreativeId = this.GetPrimaryKey(), CreativeName = State.AgentName, Answer = answer });
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(CreativeAnswerCompleteGEvent @event)
    {
        if (@event.CreativeId == this.GetPrimaryKey())
        {
            return;
        }
        
        if (@event.CreativeName.IsNullOrWhiteSpace())
        {
            return;
        }

        RaiseEvent(new AddHistoryChatSEvent()
        {
            Message = new MicroAIMessage(Role.User.ToString(),
                AssembleMessageUtil.AssembleCreativeAnswer(@event.CreativeName, @event.Answer))
        });
    }

    public Task<MicroAIGAgentState> GetStateAsync()
    {
        throw new NotImplementedException();
    }

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    public async Task SetAgent(string agentName, string agentResponsibility)
    {
        RaiseEvent(new SetAgentInfoSEvent { AgentName = agentName, Description = agentResponsibility });
        await base.ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(agentName).SetAgentAsync(agentResponsibility);
    }

    public async Task SetAgentWithTemperatureAsync(string agentName, string agentResponsibility, float temperature,
        int? seed = null,
        int? maxTokens = null)
    {
        RaiseEvent(new SetAgentInfoSEvent { AgentName = agentName, Description = agentResponsibility });
        await base.ConfirmEvents();

        await GrainFactory.GetGrain<IChatAgentGrain>(agentName)
            .SetAgentWithTemperature(agentResponsibility, temperature, seed, maxTokens);
    }

    public Task<MicroAIGAgentState> GetAgentState()
    {
        throw new NotImplementedException();
    }

    public Task<string> GetCreativeNaming()
    {
        return Task.FromResult(State.Naming);
    }

    public Task<string> GetCreativeName()
    {
        return Task.FromResult(State.AgentName);
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

public class CreativeGroupSummary
{
    [JsonPropertyName(@"name")] public string Name { get; set; }

    [JsonPropertyName(@"reason")] public string Reason { get; set; }
}