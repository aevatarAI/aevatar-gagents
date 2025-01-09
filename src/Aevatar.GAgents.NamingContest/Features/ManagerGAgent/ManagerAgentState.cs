using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.NamingContest.ManagerGAgent;

[GenerateSerializer]
public class ManagerAgentState: StateBase
{
    [Id(0)] public List<string> CreativeAgentIdList { get; set; } = new List<string>();
    
    [Id(1)] public List<string> JudgeAgentIdList { get; set; } = new List<string>();
    
    [Id(2)] public List<string> HostAgentIdList { get; set; } = new List<string>();
    
    
    [Id(3)] public Dictionary<string, InitNetWorkMessageSEvent> NetworkDictionary { get; set; } = new Dictionary<string, InitNetWorkMessageSEvent>();

    
    

    public void Apply(InitAgentMessageSEvent initAgentMessageSEvent)
    {
        CreativeAgentIdList.AddRange(initAgentMessageSEvent.CreativeAgentIdList);
        JudgeAgentIdList.AddRange(initAgentMessageSEvent.JudgeAgentIdList);
        HostAgentIdList.AddRange(initAgentMessageSEvent.HostAgentIdList);
    }
    
    public void Apply(InitNetWorkMessageSEvent initNetWorkMessageSEvent)
    {
        NetworkDictionary[initNetWorkMessageSEvent.GroupAgentId] = initNetWorkMessageSEvent;
    }
    
    public void Apply(ClearAllAgentMessageSEvent initNetWorkMessageSEvent)
    {
        CreativeAgentIdList.Clear();
        JudgeAgentIdList.Clear();
        HostAgentIdList.Clear();
    }
    
    public void Apply(ClearAllNetWorkMessageSEvent initNetWorkMessageSEvent)
    {
        NetworkDictionary = new Dictionary<string, InitNetWorkMessageSEvent>();
    }
    
    

}

