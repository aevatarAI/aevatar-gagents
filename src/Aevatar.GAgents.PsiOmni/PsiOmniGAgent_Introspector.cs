using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aevatar.GAgents.PsiOmni;

public partial class PsiOmniGAgent
{
    private Kernel GetKernel_Introspector()
    {
        var kernel = _kernelFactory.CreateKernel(
            State.Configuration!
        );
        if (kernel == null)
            throw new InvalidOperationException("Kernel is not configured for tool execution.");

        return kernel;
    }

    private async Task RunIntrospectionAsync()
    {
        var kernel = GetKernel_Introspector();
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(IntrospectorSystemPrompt);
        chatHistory.AddUserMessage(
            $"Prepare a description for the agent with the following child agents:\n{GetChildrenDescriptions()}");
        // 1. 获取 chat completion 服务
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        // 2. 构造 PromptExecutionSettings
        var maxTokens = 4000; // 默认最大 token
        var temperature = 0.1; // 默认温度
        // 只用 OpenAI 版本（无 config.Model 判断）
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            MaxTokens = maxTokens,
            Temperature = temperature
        };
        var result = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);
        chatHistory.Add(result);
        if (result.Content != null)
            RaiseEvent(new UpdateSelfDescription
            {
                Description = result.Content
            });
    }

    private string GetChildrenDescriptions()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return serializer.Serialize(State.ChildAgents.Values.ToList());
    }
}