using System;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AI.BrainFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.SyncWork;

namespace Aevatar.AI.Feature.SyncLLMWorker;

public class SyncLLMWorker : AevatarSyncWorker<SyncLLMRequestEvent, SyncLLMResponseEvent>
{
    private readonly IBrainFactory _brainFactory;

    public SyncLLMWorker(ILogger<AevatarSyncWorker<SyncLLMRequestEvent, SyncLLMResponseEvent>> logger,
        LimitedConcurrencyLevelTaskScheduler limitedConcurrencyScheduler) : base(logger, limitedConcurrencyScheduler)
    {
        _brainFactory = ServiceProvider.GetRequiredService<IBrainFactory>();
    }

    protected override async Task<SyncLLMResponseEvent> PerformLongRunTask(SyncLLMRequestEvent request)
    {
        var result = new SyncLLMResponseEvent() { RequestId = request.RequestId, ContextDto = request.ContextDto };
        try
        {
            IBrain? _brain = _brainFactory.GetBrain(request.LLMConfig);

            if (_brain == null)
            {
                Logger.LogError(
                    $"[SyncLLMWorker][PerformLongRunTask] _brain == null, llmprovider:{request.LLMConfig.ProviderEnum.ToString()}, llmModel:{request.LLMConfig.ModelIdEnum.ToString()}");
                result.ErrorMessage =
                    $"llmprovider:{request.LLMConfig.ProviderEnum.ToString()}, llmModel:{request.LLMConfig.ModelIdEnum.ToString()} not exist";
                return result;
            }

            await _brain.InitializeAsync(request.LLMConfig, request.GrainId, request.Instructions);
            var response = await _brain.InvokePromptAsync(request.Prompt, request.History, request.IfUseKnowledge,
                request.PromptSettings);
            result.ChatResponseList = response!.ChatReponseList;
            result.TokenUsageStatistics = response.TokenUsageStatistics;
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError($"llmprovider:{request.LLMConfig.ProviderEnum.ToString()}, llmModel:{request.LLMConfig.ModelIdEnum.ToString()} Exception:{ex.ToString()}");
            result.ErrorMessage =
                $"llmprovider:{request.LLMConfig.ProviderEnum.ToString()}, llmModel:{request.LLMConfig.ModelIdEnum.ToString()} Exception:{ex.ToString()}";

            return result;
        }
    }
}