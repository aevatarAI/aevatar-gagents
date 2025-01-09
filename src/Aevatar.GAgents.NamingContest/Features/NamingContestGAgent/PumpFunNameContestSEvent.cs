using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.NamingContest.GEvents;

[GenerateSerializer]
public abstract class PumpFunNameContestSEvent:GEventBase
{
   
}


[GenerateSerializer]
public class IniNetWorkMessagePumpFunSEvent : PumpFunNameContestSEvent
{
    [Id(0)] public List<string> CreativeAgentIdList { get; set; } 
    
    [Id(1)] public List<string> JudgeAgentIdList { get; set; } 
    
    [Id(2)] public List<string> ScoreAgentIdList { get; set; } 
    
    [Id(3)] public List<string> HostAgentIdList { get; set; } 
    
    
    [Id(4)] public string CallBackUrl { get; set; }
    
    [Id(5)] public string Name { get; set; }
    
    [Id(6)] public string Round { get; set; }
    
    [Id(7)] public Guid GroupId { get; set; }
 
}