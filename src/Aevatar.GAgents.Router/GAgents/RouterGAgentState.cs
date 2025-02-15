using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Router.GAgents;

[GenerateSerializer]
public class RouterGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; }
    
}