using AISmart.GAgent.Autogen;
using Moq;
using AutoGen.Core;
using AutoGen.OpenAI;
using Volo.Abp.DependencyInjection;

public class TestChatAgentProvider : IChatAgentProvider, ITransientDependency
{
    public async Task<IMessage> SendAsync(string agentName, string message, IEnumerable<IMessage> chatHistory)
    {
        string jsonResponse = @"{
    ""compete"": ""This is a mock completion message.""
}";
        var response = new Message(Role.System, jsonResponse);
        return response;
    }

    public MiddlewareAgent<MiddlewareStreamingAgent<OpenAIChatAgent>> GetAgent(string agentName)
    {
        var mock = new Mock<MiddlewareAgent<MiddlewareStreamingAgent<OpenAIChatAgent>>>();
        return mock.Object;
    }

    public void SetAgent(string agentName, string systemMessage, FunctionCallMiddleware middleware)
    {
    }
}