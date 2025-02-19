using System;

namespace Aevatar.GAgents.AI.Common;

public class TokenUsage
{
    public int InputToken { get; set; }
    public int OutputToken { get; set; }
    public int TotalUsageToken { get; set; }
    public long CreateTime { get; set; }
}