using System.Collections.Generic;
using AevatarGAgents.Autogen.Common;
using Orleans;

namespace AevatarGAgents.Autogen.EventSourcingEvent;

[GenerateSerializer]
public class Create:AutogenEventBase
{
    [Id(0)] public List<AutogenMessage> Messages { get; set; } = new List<AutogenMessage>();
    [Id(1)] public long CreateTime { get; set; }
}