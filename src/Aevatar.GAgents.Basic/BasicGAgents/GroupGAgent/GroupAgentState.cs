using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.GAgents.Common.GroupGAgent;

[GenerateSerializer]
public class GroupAgentState : StateBase
{
    [Id(0)]  public int RegisteredAgents { get; set; } = 0;
}