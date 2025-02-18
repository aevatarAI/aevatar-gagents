using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Basic;

[GenerateSerializer]
public class UserGAgentState : StateBase
{

}

[GenerateSerializer]
public class UserStateLogEvent : StateLogEventBase<UserStateLogEvent>
{

}

[GAgent]
public class UserGAgent : GAgentBase<UserGAgentState, UserStateLogEvent>, IUserGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This is a GAgent stands for user.");
    }
}