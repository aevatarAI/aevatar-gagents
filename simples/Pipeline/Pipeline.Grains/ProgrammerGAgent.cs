using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Pipeline.Abstract;
using Microsoft.Extensions.Logging;

namespace Pipeline.Grains;

public class ProgrammerGAgent : GAgentBase<ProgrammerGAgentState, ProgrammerGAgentLogEvent>, IProgrammerGAgent
{
    public ProgrammerGAgent(ILogger<ProgrammerGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    public Task<JobProgressResult<ProgramCode>> ProcessAsync(FrameworkDesign param)
    {
        Console.WriteLine("this is Programmer design");
        return Task.FromResult(new JobProgressResult<ProgramCode>()
            { Result = new ProgramCode() { Content = "this is Programmer design" } });
    }
}

[GenerateSerializer]
public class ProgrammerGAgentState : StateBase
{
}

[GenerateSerializer]
public class ProgrammerGAgentLogEvent : StateLogEventBase<ProgrammerGAgentLogEvent>
{
}

public interface IProgrammerGAgent : IJob<FrameworkDesign, ProgramCode>
{
}

[GenerateSerializer]
public class ProgramCode
{
    public string Content { get; set; }
}