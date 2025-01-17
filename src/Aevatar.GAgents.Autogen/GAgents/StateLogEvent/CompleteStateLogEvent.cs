using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class CompleteStateLogEvent:AutogenStateLogEvent
{
    [Id(0)] public string Summary { get; set; }
}