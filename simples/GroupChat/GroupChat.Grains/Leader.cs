using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Microsoft.Extensions.Logging;

namespace GroupChat.Grain;

public class Leader : GroupMemberGAgent, ILeader
{
    public Leader(ILogger<Leader> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Leader");
    }

    protected override Task<int> EvaluationInterestValueAsync(Guid blackboardId, List<ChatMessage> messages)
    {
        if (messages.Count > 10)
        {
            return Task.FromResult(100);
        }

        return Task.FromResult(0);
    }

    protected override Task<TalkResponse> SpeechAsync(Guid blackboardId, List<ChatMessage> messages)
    {
        var response = new TalkResponse();
        response.IfContinue = false;
        response.SpeakContent = "Discussion ended";
        Console.WriteLine($"{State.MemberName} Can Speak");
        return Task.FromResult(response);
    }

    protected override Task GroupChatFinishAsync(Guid blackboardId)
    {
        Console.WriteLine($"{State.MemberName} receive finish message");
        return Task.CompletedTask;
    }
}

public interface ILeader : IGroupMember
{
}