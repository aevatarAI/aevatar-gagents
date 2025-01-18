using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aevatar.GAgents.Basic;
using Aevatar.GAgents.MicroAI.Model;
using AutoGen.Core;
using AutoGen.SemanticKernel;
using AutoGen.SemanticKernel.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aevatar.GAgents.MicroAI.GAgent;

[StorageProvider(ProviderName = "PubSubStore")]
public class ChatAgentGrain : Grain, IChatAgentGrain
{
    private MiddlewareStreamingAgent<SemanticKernelAgent>? _agent;
    private readonly MicroAIOptions _options;
    private readonly AIModelOptions _aiModelOptions;
    private readonly ILogger<ChatAgentGrain> _logger;
    // private readonly ICQRSProvider _cqrsProvider;

    private IStreamProvider StreamProvider => this.GetStreamProvider(CommonConstants.StreamProvider);
    

    public ChatAgentGrain(IOptions<MicroAIOptions> options, 
        IOptions<AIModelOptions> aiModelOptions, 
        ILogger<ChatAgentGrain> logger)

    {
        _options = options.Value;
        _aiModelOptions = aiModelOptions.Value;
        _logger = logger;
        // _cqrsProvider = cqrsProvider;
    }

    public async Task<MicroAIMessage?> SendAsync(string message, List<MicroAIMessage>? chatHistory)
    {
        if (_agent != null)
        {
            var history = ConvertMessage(chatHistory);
            var imMessage = await _agent.SendAsync(message, history);
            _ = SaveAIChatLogAsync(message, imMessage.GetContent());
            return new MicroAIMessage("assistant", imMessage.GetContent()!);
        }

        _logger.LogWarning($"[ChatAgentGrain] Agent is not set");
        return null;
    }
    
    private async Task SaveAIChatLogAsync(string message, string? response)
    {
        // var command = new SaveLogCommand
        // {
        //     AgentId = this.GetGrainId().ToString(),
        //     AgentName = this.GetPrimaryKeyString(),
        //     Request = message,
        //     Response = response,
        //     Ctime = DateTime.UtcNow
        // };
        // await _cqrsProvider.SendLogCommandAsync(command);
    }

    public async Task SendEventAsync(string message, List<MicroAIMessage>? chatHistory,object requestEvent)
    {
        var agentGuid = this.GetPrimaryKeyString();
        var streamId = StreamId.Create(CommonConstants.StreamNamespace, agentGuid);
        var stream = StreamProvider.GetStream<MicroAIEventMessage>(streamId);
        MicroAIMessage? microAIMessage = null;

        try
        {
            microAIMessage = (await SendAsync(message, chatHistory))!;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[ChatAgentGrain] SendEventAsync error");
        }

        await stream.OnNextAsync(new MicroAIEventMessage(microAIMessage, requestEvent));

        
       
    }

    public Task SetAgentAsync(string systemMessage)
    {
        var kernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_options.Model, _options.Endpoint, _options.ApiKey);
        var systemName = this.GetPrimaryKeyString();
        var kernel = kernelBuilder.Build();
        var kernelAgent = new SemanticKernelAgent(
                kernel: kernel,
                name: systemName,
                systemMessage: systemMessage)
            .RegisterMessageConnector();

        _agent = kernelAgent;
        return Task.CompletedTask;
    }

    public Task SetAgentWithRandomLLMAsync(string systemMessage)
    {
        string llm = GetRandomLlmType();
        return SetAgentAsync(systemMessage, llm);
    }

    public Task SetAgentAsync(string systemMessage, string llm)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        ConfigureKernelBuilder(kernelBuilder, llm, _aiModelOptions);


        var kernel = kernelBuilder.Build();
        var systemName = this.GetPrimaryKeyString();
        var kernelAgent = new SemanticKernelAgent(
                kernel: kernel,
                name: systemName,
                systemMessage: systemMessage)
            .RegisterMessageConnector();

        _agent = kernelAgent;
        return Task.CompletedTask;
    }

    public Task SetAgentWithTemperature(string systemMessage, float temperature, int? seed = null,
        int? maxTokens = null)
    {
        var kernelBuilder = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_options.Model, _options.Endpoint, _options.ApiKey);
        var systemName = this.GetPrimaryKeyString();
        var kernel = kernelBuilder.Build();
        var kernelAgent = new SemanticKernelAgent(
                kernel: kernel,
                name: systemName,
                systemMessage: systemMessage)
            .RegisterMessageConnector();

        _agent = kernelAgent;
        return Task.CompletedTask;
    }

    private List<IMessage> ConvertMessage(List<MicroAIMessage>? listAutoGenMessage)
    {
        var result = new List<IMessage>();
        if (listAutoGenMessage != null)
        {
            foreach (var item in listAutoGenMessage)
            {
                result.Add(new TextMessage(GetRole(item.Role), item.Content));
            }
        }

        return result;
    }

    private Role GetRole(string roleName)
    {
        switch (roleName)
        {
            case "user":
                return Role.User;
            case "assistant":
                return Role.Assistant;
            case "system":
                return Role.System;
            case "function":
                return Role.Function;
            default:
                return Role.User;
        }
    }

    private static string GetRandomLlmType()
    {
        var random = new Random();
        return LLMTypesConstant.AllSupportedLlmTypes[random.Next(LLMTypesConstant.AllSupportedLlmTypes.Count)];
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
                    _options.Model, _options.Endpoint, _options.ApiKey);
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

[GenerateSerializer]
public class MicroAIEventMessage
{
    [Id(0)] public MicroAIMessage MicroAIMessage { get; set; }
    
    [Id(1)] public object Event { get; set; }

    public MicroAIEventMessage(MicroAIMessage microAIMessage, object @event)
    {
        // Validate the input parameters to ensure non-null references
        MicroAIMessage = microAIMessage;
        Event = @event;
    }
}