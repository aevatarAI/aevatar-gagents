using Aevatar.Core;
using GroupChat.GAgent.Feature.Coordinator.LogEvent;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using GroupChat.GAgent.GEvent;

namespace GroupChat.GAgent.Feature.Coordinator;

public abstract class CoordinatorGAgentBase : GAgentBase<CoordinatorStateBase, CoordinatorLogEventBase>, ICoordinatorGAgent
{
    private IDisposable _timer;
    private List<InterestInfo> _interestInfoList = new List<InterestInfo>();
    private List<GroupMember> _groupMembers = new List<GroupMember>();

    public CoordinatorGAgentBase(ILogger logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Blackboard coordinator");
    }

    public async Task HandleSpeechResponseEventAsync(SpeechResponseEvent @event)
    {
        if (@event.BlackboardId != this.GetPrimaryKey())
        {
            return;
        }

        RaiseEvent(new AddChatTermLogEvent());
        await ConfirmEvents();

        _interestInfoList.Clear();

        // group chat finished
        if (@event.TalkResponse.IfContinue == false)
        {
            await PublishAsync(new GroupChatFinishEvent() { BlackboardId = this.GetPrimaryKey() });
            return;
        }

        // next round
        if (await NeedGetMemberInterestValue(_groupMembers, this.GetPrimaryKey()))
        {
            await PublishAsync(new EvaluationInterestEvent()
                { BlackboardId = this.GetPrimaryKey(), ChatTerm = State.ChatTerm });
        }
        else
        {
            await Coordinator();
        }
    }

    public async Task HandleEvaluationInterestResultEventAsync(EvaluationInterestResultEvent @event)
    {
        if (@event.BlackboardId != this.GetPrimaryKey() || @event.ChatTerm < State.ChatTerm)
        {
            return;
        }

        _interestInfoList.Add(new InterestInfo() { MemberId = @event.MemberId, InterestValue = @event.InterestValue });
    }

    public async Task HandleCoordinatorPongEventAsync(CoordinatorPongEvent @event)
    {
        if (@event.BlackboardId != this.GetPrimaryKey())
        {
            return;
        }

        var member = _groupMembers.Find(f => f.Id == @event.MemberId);
        if (member == null)
        {
            member = new GroupMember() { Id = @event.MemberId, Name = @event.MemberName };
            _groupMembers.Add(member);
        }

        member.UpdateTime = DateTime.Now;
    }

    protected async virtual Task<Guid> CoordinatorToSpeak(List<InterestInfo> interestInfos, List<GroupMember> members)
    {
        var randList = new List<Guid>();
        if (interestInfos.Count > 0)
        {
            var interestInfo = interestInfos.OrderByDescending(o => o.InterestValue).ToList();
            if (interestInfo[0].InterestValue == 100)
            {
                return interestInfo[0].MemberId;
            }

            randList = interestInfos.Take(5).Select(s=>s.MemberId).ToList();
        }

        if (randList.Count == 0)
        {
            randList = members.Select(s => s.Id).ToList();
        }

        if (members.Count == 0)
        {
            return Guid.Empty;
        }

        var random = new Random();
        var randomNum = random.Next(0, randList.Count);

        return randList[randomNum];
    }

    protected virtual Task<bool> NeedGetMemberInterestValue(List<GroupMember> members, Guid blackboardId)
    {
        return Task.FromResult(false);
    }

    protected virtual Task<bool> CheckSendCoordinatorPingEventAsync(DateTime dateTime)
    {
        return Task.FromResult(dateTime.Second % 10 == 0);
    }

    protected virtual Task<bool> CheckMemberOutOfGroupAsync(GroupMember groupMember)
    {
        return Task.FromResult((DateTime.Now - groupMember.UpdateTime).Seconds < 20);
    }

    protected virtual Task<bool> NeedSelectSpeaker(List<InterestInfo> interestInfos, List<GroupMember> members)
    {
        return Task.FromResult(interestInfos.Count >= (members.Count * 2) / 3);
    }
    
    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        _timer = this.RegisterGrainTimer(CoordinatorPing, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        return Task.CompletedTask;
    }

    private async Task CoordinatorPing(CancellationToken token)
    {
        if (await NeedGetMemberInterestValue(_groupMembers, this.GetPrimaryKey()))
        {
            if (await NeedSelectSpeaker(_interestInfoList, _groupMembers))
            {
                await Coordinator();
            }
        }
        
        var ifSendPingMsg = await CheckSendCoordinatorPingEventAsync(DateTime.Now);
        if (ifSendPingMsg)
        {
            await PublishAsync(new CoordinatorPingEvent() { BlackboardId = this.GetPrimaryKey() });
            var leaveMember = new List<Guid>();
            foreach (var member in _groupMembers)
            {
                var ifInGroup = await CheckMemberOutOfGroupAsync(member);
                if (ifInGroup == false)
                {
                    leaveMember.Add(member.Id);
                }
            }

            if (leaveMember.Count > 0)
            {
                _groupMembers.RemoveAll(f => leaveMember.Contains(f.Id));
            }
        }
    }
    
    private async Task Coordinator()
    {
        var speaker = await CoordinatorToSpeak(_interestInfoList, _groupMembers);
        if (speaker == Guid.Empty)
        {
            return;
        }
        
        await PublishAsync(new SpeechEvent() { BlackboardId = this.GetPrimaryKey(), Speaker = speaker });
    }

}

public interface ICoordinatorGAgent : IGAgent
{
    Task HandleSpeechResponseEventAsync(SpeechResponseEvent @event);
    Task HandleEvaluationInterestResultEventAsync(EvaluationInterestResultEvent @event);
    Task HandleCoordinatorPongEventAsync(CoordinatorPongEvent @event);
}