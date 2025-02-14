using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Microsoft.Extensions.Logging;

namespace GroupChat.Grain;

public class Worker : GroupMemberGAgent, IWorker
{
    public Worker(ILogger<GroupMemberGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("you are worker");
    }

    protected override Task<int> EvaluationInterestValueAsync(Guid blackboardId, List<ChatMessage> messages)
    {
        var random = new Random();

        return Task.FromResult(random.Next(1, 90));
    }

    protected override Task<TalkResponse> SpeechAsync(Guid blackboardId, List<ChatMessage> messages)
    {
        var response = new TalkResponse();
        response.SpeakContent = $"this is the {messages.Count} message";
        
        Console.WriteLine($"{State.MemberName} Can Speak");
        return Task.FromResult(response);
    }
    
    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        Console.WriteLine($"{State.MemberName} receive finish message");
        return Task.CompletedTask;
    }
}

public interface IWorker : IGroupMember
{
}