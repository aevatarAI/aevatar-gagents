using System;
using Aevatar.GAgents.MicroAI.GAgent;
using AutoGen.Core;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using AutoGen.SemanticKernel;
using AutoGen.SemanticKernel.Extension;

namespace Aevatar.GAgents.MicroAI;

public class KernelAgentFactory : IKernelAgentFactory
{
    private readonly AIModelOptions _aiModelOptions;

    public KernelAgentFactory(IOptions<AIModelOptions> aiModelOptions
        )

    {
        _aiModelOptions = aiModelOptions.Value;
    }
    
    public MiddlewareStreamingAgent<SemanticKernelAgent> CreateAgent(string systemName, string llmType,
        string systemMessage)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        ConfigureKernelBuilder(kernelBuilder, llmType, _aiModelOptions);

        var kernel = kernelBuilder.Build();
        var kernelAgent = new SemanticKernelAgent(
                kernel: kernel,
                name: systemName,
                systemMessage: systemMessage)
            .RegisterMessageConnector();
        return kernelAgent;
    }

    private void ConfigureKernelBuilder(IKernelBuilder kernelBuilder, string llm, AIModelOptions aiModelOptions)
    {
        switch (llm)
        {
            case LLMTypesConstant.AzureOpenAI:
            {
                // Fetch Azure-specific configuration.
                var azureOptions = aiModelOptions.AzureOpenAI;

                // Add Azure OpenAI Chat Completion to the KernelBuilder.
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    azureOptions.Model, azureOptions.Endpoint, azureOptions.ApiKey);
                break;
            }

            case LLMTypesConstant.Bedrock:
            {
                // Fetch Bedrock-specific configuration.
                var bedrockOptions = aiModelOptions.Bedrock;

                #pragma warning disable SKEXP0070

                // Add Bedrock Chat Completion to the KernelBuilder.
                kernelBuilder.AddBedrockChatCompletionService(
                    modelId: bedrockOptions.Model,
                    serviceId: bedrockOptions.ServiceId
                );
                #pragma warning restore SKEXP0070

                break;
            }

            case LLMTypesConstant.GoogleGemini:
            {
                // Fetch Google Gemini-specific configuration.
                var googleOptions = aiModelOptions.GoogleGemini;

                #pragma warning disable SKEXP0070
                // Add Google Gemini Chat Completion to the KernelBuilder.
                kernelBuilder.AddGoogleAIGeminiChatCompletion(
                    modelId: googleOptions.Model,
                    apiKey: googleOptions.ApiKey,
                    apiVersion: GoogleAIVersion.V1, // Optional: API version.
                    serviceId: googleOptions.ServiceId // Optional: Target a specific service.
                );
                #pragma warning restore SKEXP0070

                break;
            }

            default:
                throw new ArgumentException($"Unsupported LLM type: {llm}");
        }
    }
}