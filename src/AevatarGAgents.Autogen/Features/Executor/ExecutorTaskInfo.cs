using System;
using System.Collections.Generic;
using AevatarGAgents.Autogen.Common;
using AutoGen.Core;
using Orleans;

namespace AevatarGAgents.Autogen.Executor;

[GenerateSerializer]
public class ExecutorTaskInfo
{
   [Id(0)] public Guid TaskId { get; set; }
   [Id(1)] public List<AutogenMessage> History { get; set; }
   [Id(2)] public string AgentDescriptionManagerId { get; set; }
}