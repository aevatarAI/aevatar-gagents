using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.GraphRag.Abstractions;
using Aevatar.GAgents.GraphRag.Abstractions.Extensions;
using Aevatar.GAgents.Neo4j.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.AIGAgent.Agent;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent> : AIGAgentBase<TState, TStateLogEvent, EventBase, ConfigurationBase>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent> : AIGAgentBase<TState, TStateLogEvent, TEvent, ConfigurationBase>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase;

public abstract partial class
    AIGAgentBase<TState, TStateLogEvent, TEvent, TConfiguration> :
    GAgentBase<TState, TStateLogEvent, TEvent, TConfiguration>, IAIGAgent
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
    where TConfiguration : ConfigurationBase
{
    private readonly IBrainFactory _brainFactory;
    private IBrain? _brain = null;
    private readonly IGraphRagStore _graphRagStore;

    public AIGAgentBase()
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
        _graphRagStore = ServiceProvider.GetRequiredService<IGraphRagStore>();
    }

    public async Task<bool> InitializeAsync(InitializeDto initializeDto)
    {
        //save state
        await AddLLMAsync(initializeDto.LLM);
        await AddPromptTemplateAsync(initializeDto.Instructions);

        return await InitializeBrainAsync(initializeDto.LLM, initializeDto.Instructions, State.IfUpsertKnowledge);
    }

    public async Task<bool> UploadKnowledge(List<BrainContentDto>? knowledgeList)
    {
        if (_brain == null)
        {
            return false;
        }

        if (knowledgeList == null || !knowledgeList.Any())
        {
            return true;
        }

        if (State.IfUpsertKnowledge == false)
        {
            RaiseEvent(new SetUpsertKnowledgeFlag());
            await ConfirmEvents();
        }

        List<BrainContent> fileList = knowledgeList.Select(f => f.ConvertToBrainContent()).ToList();
        return await _brain.UpsertKnowledgeAsync(fileList);
    }

    public async Task SetGraphRagRetrieveInfo(string schema, string? example)
    {
        if (schema.IsNullOrEmpty())
        {
            return;
        }
        
        RaiseEvent(new SetGraphRagSchemaLogEvent
        {
            Schema = schema,
            Example = example??""
        });
        await ConfirmEvents(); 
    }

    private async Task<bool> InitializeBrainAsync(string LLM, string systemMessage, bool ifSupportKnowledge = false)
    {
        _brain = _brainFactory.GetBrain(LLM);

        if (_brain == null)
        {
            Logger.LogError("Failed to initialize brain. {@LLM}", LLM);
            return false;
        }

        // remove slash from this.GetGrainId().ToString() so that it can be used as the collection name pertaining to the grain
        var grainId = this.GetGrainId().ToString().Replace("/", "");

        await _brain.InitializeAsync(grainId, systemMessage);

        return true;
    }

    private async Task AddLLMAsync(string LLM)
    {
        if (State.LLM == LLM)
        {
            Logger.LogError("Cannot add duplicate LLM: {LLM}.", LLM);
            return;
        }

        RaiseEvent(new SetLLMStateLogEvent
        {
            LLM = LLM
        });
        await ConfirmEvents();
    }
    
    private async Task<string> GraphRagDataAsync(string text)
    {
        var prompt = Prompts.Text2CypherTemplate
            .Replace("{schema}", State.RetrieveSchema)
            .Replace("{examples}", State.RetrieveExample)
            .Replace("{query_text}", text);
        var invokeResponse = await _brain.InvokePromptAsync(prompt, null, false);
        
        if (invokeResponse == null)
        {
            return string.Empty;
        }
        
        var cypher = invokeResponse.ChatReponseList?[0].Content;

        if (cypher.IsNullOrEmpty())
        {
            Logger.LogError("Cannot generate cypher from text: {text}.", text);
            return null;
        }
        
        var result = await _graphRagStore.QueryAsync(cypher);
        if (!result.Any())
        {
            return string.Empty;
        }
        
        return result.ToNaturalLanguage();
    }

    [GenerateSerializer]
    public class SetLLMStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public required string LLM { get; set; }
    }

    [GenerateSerializer]
    public class SetUpsertKnowledgeFlag : StateLogEventBase<TStateLogEvent>
    {
    }
    
    [GenerateSerializer]
    public class SetGraphRagSchemaLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public required string Schema { get; set; }
        [Id(1)] public string Example { get; set; } 
    }

    private async Task AddPromptTemplateAsync(string promptTemplate)
    {
        RaiseEvent(new SetPromptTemplateStateLogEvent
        {
            PromptTemplate = promptTemplate
        });
        await ConfirmEvents();
    }

    [GenerateSerializer]
    public class SetPromptTemplateStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public required string PromptTemplate { get; set; }
    }

    [GenerateSerializer]
    public class TokenUsageStateLogEvent : StateLogEventBase<TStateLogEvent>
    {
        [Id(0)] public Guid GrainId { get; set; }
        [Id(1)] public int InputToken { get; set; }
        [Id(2)] public int OutputToken { get; set; }
        [Id(3)] public int TotalUsageToken { get; set; }
        [Id(4)] public long CreateTime { get; set; }
    }

    protected async Task<List<ChatMessage>?> ChatWithHistory(string prompt, List<ChatMessage>? history = null)
    {
        if (_brain == null)
        {
            return null;
        }

        if (!State.RetrieveSchema.IsNullOrEmpty())
        {
            var graphRagData = await GraphRagDataAsync(prompt);
            if (!graphRagData.IsNullOrEmpty())
            {
                if (history == null)
                {
                    history = new List<ChatMessage>();
                }
                
                history.Add(new ChatMessage
                {
                    ChatRole = ChatRole.User,
                    Content = graphRagData
                });
            }
        }
        
        var invokeResponse = await _brain.InvokePromptAsync(prompt, history, State.IfUpsertKnowledge);
        if (invokeResponse == null)
        {
            return null;
        }

        var tokenUsage = new TokenUsageStateLogEvent()
        {
            GrainId = this.GetPrimaryKey(),
            InputToken = invokeResponse.TokenUsageStatistics.InputToken,
            OutputToken = invokeResponse.TokenUsageStatistics.OutputToken,
            TotalUsageToken = invokeResponse.TokenUsageStatistics.TotalUsageToken,
            CreateTime = invokeResponse.TokenUsageStatistics.CreateTime
        };
        
        RaiseEvent(tokenUsage);

        return invokeResponse.ChatReponseList;
    }

    protected virtual async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Derived classes can override this method.
    }

    protected sealed override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);

        // setup brain
        if (State.LLM != string.Empty)
        {
            await InitializeBrainAsync(State.LLM, State.PromptTemplate);
        }

        await OnAIGAgentActivateAsync(cancellationToken);
    }

    protected sealed override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        switch (@event)
        {
            case SetLLMStateLogEvent setLlmStateLogEvent:
                State.LLM = setLlmStateLogEvent.LLM;
                break;
            case SetPromptTemplateStateLogEvent setPromptTemplateStateLogEvent:
                State.PromptTemplate = setPromptTemplateStateLogEvent.PromptTemplate;
                break;
            case SetUpsertKnowledgeFlag setUpsertKnowledgeFlag:
                State.IfUpsertKnowledge = true;
                break;
            case SetGraphRagSchemaLogEvent setGraphRagSchemaLogEvent:
                State.RetrieveSchema = setGraphRagSchemaLogEvent.Schema;
                State.RetrieveExample = setGraphRagSchemaLogEvent.Example;
                break;
        }

        AIGAgentTransitionState(state, @event);
    }

    protected virtual void AIGAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        // Derived classes can override this method.
    }
}