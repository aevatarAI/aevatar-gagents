using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Azure.AI.Inference;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public abstract class AzureAIInferenceBrain : BrainBase
{
    public AzureAIInferenceBrain(
        IKernelBuilderFactory kernelBuilderFactory,
        ILogger<AzureAIInferenceBrain> logger,
        IOptions<RagConfig> ragConfig)
        : base(kernelBuilderFactory, logger, ragConfig)
    {
    }

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        kernelBuilder.AddAzureAIInferenceChatCompletion(
            llmConfig.ModelName,
            llmConfig.ApiKey,
            new Uri(llmConfig.Endpoint));

        return Task.CompletedTask;
    }

    protected override PromptExecutionSettings GetPromptExecutionSettings(ExecutionPromptSettings promptSettings)
    {
        PromptExecutionSettings result = new PromptExecutionSettings();
        result.ExtensionData = new Dictionary<string, object>();
        if (promptSettings.Temperature.IsNullOrWhiteSpace() == false)
        {
            result.ExtensionData.Add("temperature", promptSettings.Temperature);
        }

        if (promptSettings.MaxToken > 0)
        {
            result.ExtensionData.Add("max_tokens", promptSettings.MaxToken);
        }

        return result;
    }

    protected override TokenUsageStatistics GetTokenUsage(IReadOnlyCollection<ChatMessageContent> messageList)
    {
        int inputUsage = 0;
        int outputUsage = 0;
        int totalUsage = 0;
        foreach (var item in messageList)
        {
            if (item.InnerContent is ChatCompletions completions)
            {
                inputUsage += completions.Usage.PromptTokens;
                outputUsage += completions.Usage.CompletionTokens;
                totalUsage += completions.Usage.TotalTokens;
            }
        }

        return new TokenUsageStatistics()
        {
            InputToken = inputUsage, OutputToken = outputUsage, TotalUsageToken = totalUsage,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }
}