using System.Runtime.InteropServices.JavaScript;
using Aevatar.Core;
using GroupChat.GAgent.Feature.Coordinator.LogEvent;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using GroupChat.GAgent.GEvent;

namespace GroupChat.GAgent.Feature.Coordinator;

public abstract class CoordinatorGAgentBase : GAgentBase<CoordinatorStateBase, CoordinatorLogEventBase>,
    ICoordinatorGAgent
{
    private IDisposable? _timer;
    private List<InterestInfo> _interestInfoList = new List<InterestInfo>();
    private List<GroupMember> _groupMembers = new List<GroupMember>();
    private DateTime _latestSendInterestTime = DateTime.Now;

    public CoordinatorGAgentBase(ILogger<CoordinatorGAgentBase> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Blackboard coordinator");
    }

    public Task StartAsync()
    {
        TryStartTimer();

        return Task.CompletedTask;
    }

    public Task<bool> CheckChatIsAvailable(ChatResponseEvent @event)
    {
        return Task.FromResult(@event.BlackboardId == this.GetPrimaryKey() && @event.Term == State.ChatTerm &&
                               @event.MemberId == State.CoordinatorSpeaker);
    }

    public async Task HandleChatResponseEventAsync(ChatResponseEvent @event)
    {
        if (@event.BlackboardId != this.GetPrimaryKey())
        {
            return;
        }

        RaiseEvent(new AddChatTermLogEvent() { IfComplete = @event.ChatResponse.Continue });
        await ConfirmEvents();

        _interestInfoList.Clear();

        // group chat finished
        if (@event.ChatResponse.Continue == false)
        {
            await PublishAsync(new GroupChatFinishEventForCoordinator() { BlackboardId = this.GetPrimaryKey() });
            if (_timer != null)
            {
                _timer.Dispose();
            }

            return;
        }

        // next round
        if (await NeedCheckMemberInterestValue(_groupMembers, this.GetPrimaryKey()))
        {
            await PublishAsync(new EvaluationInterestEventForCoordinator()
                { BlackboardId = this.GetPrimaryKey(), ChatTerm = State.ChatTerm });
            _latestSendInterestTime = DateTime.Now;
        }
        else
        {
            await Coordinator();
        }
    }

    public async Task HandleGetInterestResultEventAsync(EvaluationInterestResponseEvent @event)
    {
        if (@event.BlackboardId != this.GetPrimaryKey() || @event.ChatTerm < State.ChatTerm)
        {
            return;
        }

        var member = _interestInfoList.Find(f => f.MemberId == @event.MemberId);
        if (member == null)
        {
            _interestInfoList.Add(new InterestInfo()
                { MemberId = @event.MemberId, InterestValue = @event.InterestValue });
        }
        else
        {
            member.InterestValue = @event.InterestValue;
        }
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

            randList = interestInfos.Take(5).Select(s => s.MemberId).ToList();
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

    protected virtual Task<bool> NeedCheckMemberInterestValue(List<GroupMember> members, Guid blackboardId)
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
        TryStartTimer();
        return Task.CompletedTask;
    }

    private void TryStartTimer()
    {
        _timer ??= this.RegisterGrainTimer(BackgroundWork, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private async Task BackgroundWork(CancellationToken token)
    {
        await TryDriveProgress();
        await TryPingMember();
    }

    private async Task TryDriveProgress()
    {
        if (await NeedCheckMemberInterestValue(_groupMembers, this.GetPrimaryKey()) == false)
        {
            return;
        }

        if (State.IfTriggerCoordinate == false)
        {
            var ifCoordinator = false;
            if (_groupMembers.Count > 0)
            {
                var ifSelectSpeaker = await NeedSelectSpeaker(_interestInfoList, _groupMembers);
                if (ifSelectSpeaker)
                {
                    ifCoordinator = await Coordinator();
                }
            }

            if (ifCoordinator == false &&
                (DateTime.Now - _latestSendInterestTime).Seconds > 5 &&
                _interestInfoList.Count <= (_groupMembers.Count * 2) / 3)
            {
                await PublishAsync(new EvaluationInterestEventForCoordinator()
                    { BlackboardId = this.GetPrimaryKey(), ChatTerm = State.ChatTerm });
                _latestSendInterestTime = DateTime.Now;
            }
            
            return;
        }

        // member not send message, should reschedule
        if ((DateTime.Now - State.CoordinatorTime).TotalSeconds > 10)
        {
            await Coordinator();
        }
    }

    private async Task TryPingMember()
    {
        var ifSendPingMsg = await CheckSendCoordinatorPingEventAsync(DateTime.Now);
        if (ifSendPingMsg)
        {
            await PublishAsync(new CoordinatorPingEventForCoordinator() { BlackboardId = this.GetPrimaryKey() });
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

    private async Task<bool> Coordinator()
    {
        var speaker = await CoordinatorToSpeak(_interestInfoList, _groupMembers);
        if (speaker == Guid.Empty)
        {
            return false;
        }

        await PublishAsync(new ChatEventForCoordinator()
            { BlackboardId = this.GetPrimaryKey(), Speaker = speaker, Term = State.ChatTerm });

        RaiseEvent(new TriggerCoordinator() { MemberId = speaker, CreateTime = DateTime.Now });
        await ConfirmEvents();

        return true;
    }
}

public interface ICoordinatorGAgent : IGAgent
{
    Task StartAsync();
    Task<bool> CheckChatIsAvailable(ChatResponseEvent @event);
    Task HandleChatResponseEventAsync(ChatResponseEvent @event);
    Task HandleGetInterestResultEventAsync(EvaluationInterestResponseEvent @event);
    Task HandleCoordinatorPongEventAsync(CoordinatorPongEvent @event);
}