using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.NamingContest.GEvents;
using Orleans;

namespace AISmart.Agent;
[GenerateSerializer]
public class PumpFunNamingContestGAgentState : StateBase
{
    
    [Id(0)] public Dictionary<string, EventBase> ReceiveMessage { get; set; } = new Dictionary<string, EventBase>();
    
    [Id(1)] public string CallBackUrl { get; set; }
    
    [Id(2)] public string Name { get; set; }
    
    
    
    [Id(3)] public List<string> CreativeAgentIdList { get; set; } 
    
    [Id(4)] public List<string> JudgeAgentIdList { get; set; } 
    
    [Id(5)] public List<string> JudgeScoreAgentIdList { get; set; } 
    
    [Id(6)] public List<string> HostAgentIdList { get; set; } 
    
    [Id(7)] public string Round { get; set; }
    
    [Id(8)] public Guid groupId { get; set; }

    


    public void Apply(IniNetWorkMessagePumpFunSEvent iniNetWorkMessageSEvent)
    {
        CallBackUrl = iniNetWorkMessageSEvent.CallBackUrl;
        Name = iniNetWorkMessageSEvent.Name;
        Round = iniNetWorkMessageSEvent.Round;
        CreativeAgentIdList = iniNetWorkMessageSEvent.CreativeAgentIdList;
        JudgeAgentIdList = iniNetWorkMessageSEvent.JudgeAgentIdList;
        JudgeScoreAgentIdList = iniNetWorkMessageSEvent.ScoreAgentIdList;
        HostAgentIdList = iniNetWorkMessageSEvent.HostAgentIdList;
        groupId = iniNetWorkMessageSEvent.GroupId;
    }

}