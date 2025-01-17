using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class BreakStateLogEvent:AutogenStateLogEvent
{
    [Id(0)] public string BreakReason { get; set; }    
}