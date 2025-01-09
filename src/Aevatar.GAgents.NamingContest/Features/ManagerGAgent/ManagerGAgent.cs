using System;
using System.Threading.Tasks;
using Aevatar.Core;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.NamingContest.ManagerGAgent;

public class ManagerGAgent : GAgentBase<ManagerAgentState, ManagerSEvent>, IManagerGAgent
{
    

    public ManagerGAgent( ILogger<ManagerGAgent> logger) : base(logger)
    {
    }
    

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    public async Task InitAgentsAsync(InitAgentMessageSEvent initAgentMessageSEvent)
    {
        RaiseEvent(initAgentMessageSEvent);
        await base.ConfirmEvents();
        
    }

    public async Task InitGroupInfoAsync(InitNetWorkMessageSEvent initNetWorkMessageSEvent,string groupAgentId )
    {
        RaiseEvent(initNetWorkMessageSEvent);
        await base.ConfirmEvents();
        
    }

    public async Task ClearAllAgentsAsync()
    {
        RaiseEvent(new ClearAllAgentMessageSEvent());
        RaiseEvent(new ClearAllNetWorkMessageSEvent());
        await base.ConfirmEvents();
    }
    
    public async Task<ManagerAgentState> GetManagerAgentStateAsync()
    {
        return State;
    }
}