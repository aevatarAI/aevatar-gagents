using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.SemanticKernel.KernelBuilderFactory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Volo.Abp.BlobStoring;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Aevatar.GAgents.SemanticKernel.Brain;

public class OpenAIBrain : BrainBase
{
    public OpenAIBrain(IKernelBuilderFactory kernelBuilderFactory, ILogger<OpenAIBrain> logger, IOptions<RagConfig> ragConfig,
        IBlobContainer blobContainer)
        : base(kernelBuilderFactory, logger, ragConfig, blobContainer)
    {
    }

    public override LLMProviderEnum ProviderEnum => LLMProviderEnum.OpenAI;
    public override ModelIdEnum ModelIdEnum => ModelIdEnum.OpenAI;

    protected override Task ConfigureKernelBuilder(LLMConfig llmConfig, IKernelBuilder kernelBuilder)
    {
        OpenAIClientOptions? clientOptions = null;
        if (!llmConfig.Endpoint.IsNullOrWhiteSpace())
        {
            clientOptions = new OpenAIClientOptions() { 
                Endpoint = new Uri(llmConfig.Endpoint), 
                NetworkTimeout = TimeSpan.FromSeconds(llmConfig.NetworkTimeoutInSeconds) };
        }

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(llmConfig.ApiKey), clientOptions
        );

        kernelBuilder.AddOpenAIChatCompletion(llmConfig.ModelName, openAiClient);

        return Task.CompletedTask;
    }

    protected override PromptExecutionSettings GetPromptExecutionSettings(ExecutionPromptSettings promptSettings)
    {
        var result = new OpenAIPromptExecutionSettings();
        
        if (promptSettings.Temperature.IsNullOrWhiteSpace() == false)
        {
            result.Temperature = double.Parse(promptSettings.Temperature);
        }

        if (promptSettings.MaxToken > 0)
        {
            result.MaxTokens = promptSettings.MaxToken;
        }

        // Enable usage statistics in streaming responses
        result.ExtensionData = new Dictionary<string, object>
        {
            ["stream_options"] = new { include_usage = true }
        };

        return result;
    }

    protected override TokenUsageStatistics GetTokenUsage(IReadOnlyCollection<ChatMessageContent> messageList)
    {
        int inputUsage = 0;
        int outputUsage = 0;
        int totalUsage = 0;
        int cachedTokens = 0;
        
        foreach (var item in messageList)
        {
            if (item.InnerContent is ChatCompletion completions)
            {
                inputUsage += completions.Usage.InputTokenCount;
                outputUsage += completions.Usage.OutputTokenCount;
                totalUsage += completions.Usage.TotalTokenCount;
                
                // Extract cached tokens from InputTokenDetails (Prompt Caching)
                if (completions.Usage.InputTokenDetails != null)
                {
                    cachedTokens += completions.Usage.InputTokenDetails.CachedTokenCount;
                }
            }
        }

        return new TokenUsageStatistics()
        {
            InputToken = inputUsage, 
            OutputToken = outputUsage, 
            TotalUsageToken = totalUsage,
            CachedTokens = cachedTokens,
            CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

        public override TokenUsageStatistics GetStreamingTokenUsage(List<object> messageList)
        {
            int inputUsage = 0;
            int outputUsage = 0;
            int totalUsage = 0;
            int cachedTokens = 0;
            
            Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Processing {messageList.Count} messages");
            
            int processedCount = 0;
            int streamingCount = 0;
            int metadataCount = 0;
            int usageFoundCount = 0;
            
            foreach (var item in messageList)
            {
                processedCount++;
                
                if (item is StreamingChatMessageContent streamingChatMessageContent)
                {
                    streamingCount++;
                    
                    // Log Metadata status
                    bool hasMetadata = streamingChatMessageContent.Metadata != null;
                    bool hasUsageKey = hasMetadata && streamingChatMessageContent.Metadata.ContainsKey("Usage");
                    
                    if (hasMetadata) metadataCount++;
                    
                    // Print entire Metadata for debugging
                    string metadataJson = "null";
                    if (hasMetadata)
                    {
                        try
                        {
                            var metadataDict = streamingChatMessageContent.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "null");
                            metadataJson = System.Text.Json.JsonSerializer.Serialize(metadataDict);
                        }
                        catch (Exception ex)
                        {
                            metadataJson = $"Error serializing: {ex.Message}";
                        }
                    }
                    
                    Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Chunk #{processedCount}: HasMetadata={hasMetadata}, HasUsageKey={hasUsageKey}, InnerContent={streamingChatMessageContent.InnerContent?.GetType().Name ?? "null"}, Metadata={metadataJson}");
                    
                    // Try Metadata first (verified working in openai-cache-test and AzureOpenAIBrain)
                    if (hasUsageKey)
                    {
                        usageFoundCount++;
                        var usage = streamingChatMessageContent.Metadata["Usage"];
                        Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Found Usage in Metadata, Type: {usage?.GetType().FullName ?? "null"}, Value: {usage?.ToString() ?? "null"}");
                        
                        if (usage is ChatTokenUsage tokenUsage)
                        {
                            Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] ChatTokenUsage - Input:{tokenUsage.InputTokenCount}, Output:{tokenUsage.OutputTokenCount}, Total:{tokenUsage.TotalTokenCount}");
                            
                            inputUsage += tokenUsage.InputTokenCount;
                            outputUsage += tokenUsage.OutputTokenCount;
                            totalUsage += tokenUsage.TotalTokenCount;
                            
                            // Extract cached tokens from InputTokenDetails (Prompt Caching)
                            if (tokenUsage.InputTokenDetails != null)
                            {
                                cachedTokens += tokenUsage.InputTokenDetails.CachedTokenCount;
                                Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Found CachedTokens: {tokenUsage.InputTokenDetails.CachedTokenCount}");
                            }
                            else
                            {
                                Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] InputTokenDetails is NULL");
                            }
                        }
                        else
                        {
                            if (usage == null)
                            {
                                Logger.LogWarning($"[OpenAIBrain][GetStreamingTokenUsage] ⚠️ Usage key exists but value is NULL! This means OpenAI API (or proxy) did not return usage data. InnerContent: {streamingChatMessageContent.InnerContent?.GetType().FullName ?? "null"}");
                            }
                            else
                            {
                                Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Failed to cast Usage to ChatTokenUsage, actual type: {usage?.GetType().FullName ?? "null"}");
                            }
                        }
                    }
                    // Fallback to InnerContent (for compatibility)
                    else if (streamingChatMessageContent.InnerContent is ChatCompletion completions)
                    {
                        usageFoundCount++;
                        Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Found ChatCompletion in InnerContent - Input:{completions.Usage.InputTokenCount}, Output:{completions.Usage.OutputTokenCount}");
                        
                        inputUsage += completions.Usage.InputTokenCount;
                        outputUsage += completions.Usage.OutputTokenCount;
                        totalUsage += completions.Usage.TotalTokenCount;
                        
                        // Extract cached tokens from InputTokenDetails (Prompt Caching)
                        if (completions.Usage.InputTokenDetails != null)
                        {
                            cachedTokens += completions.Usage.InputTokenDetails.CachedTokenCount;
                            Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Found CachedTokens in InnerContent: {completions.Usage.InputTokenDetails.CachedTokenCount}");
                        }
                    }
                }
                else
                {
                    Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Chunk #{processedCount}: Not StreamingChatMessageContent, Type: {item?.GetType().Name ?? "null"}");
                }
            }
            
            Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Summary - Total:{processedCount}, Streaming:{streamingCount}, HasMetadata:{metadataCount}, UsageFound:{usageFoundCount}");

            Logger.LogInformation($"[OpenAIBrain][GetStreamingTokenUsage] Final result - Input:{inputUsage}, Output:{outputUsage}, Cached:{cachedTokens}");

            return new TokenUsageStatistics()
            {
                InputToken = inputUsage, 
                OutputToken = outputUsage, 
                TotalUsageToken = totalUsage,
                CachedTokens = cachedTokens,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
}