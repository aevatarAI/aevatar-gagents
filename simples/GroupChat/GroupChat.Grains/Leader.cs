using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using Microsoft.Extensions.Logging;

namespace GroupChat.Grain;

public class Leader : GroupMemberGAgentBase, ILeader
{
    public Leader(ILogger<Leader> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Leader");
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId, List<ChatMessage> messages)
    {
        if (messages.Count > 10)
        {
            return Task.FromResult(100);
        }

        return Task.FromResult(0);
    }

    protected override Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage> messages)
    {
        var response = new ChatResponse();
        if (messages.Count() < 10)
        {
            response.Skip = true;
            return Task.FromResult(response);
        }

        response.Continue = false;
        response.Content = "Discussion ended";
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