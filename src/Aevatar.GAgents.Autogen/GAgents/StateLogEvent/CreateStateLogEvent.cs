using System.Collections.Generic;
using Aevatar.GAgents.Autogen.Common;
using Orleans;

namespace Aevatar.GAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class CreateStateLogEvent:AutogenStateLogEvent
{
    [Id(0)] public List<AutogenMessage> Messages { get; set; } = new List<AutogenMessage>();
    [Id(1)] public long CreateTime { get; set; }
}