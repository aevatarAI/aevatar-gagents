using System.Collections.Generic;

namespace AevatarGAgents.NamingContest.Options;


public class NameContestOptions
{
    public Dictionary<string, string> CreativeGAgent { get; set; }
    
    public Dictionary<string, string> JudgeGAgent { get; set; }
    
    public string MostCharmingCallback  { get; set; }
}