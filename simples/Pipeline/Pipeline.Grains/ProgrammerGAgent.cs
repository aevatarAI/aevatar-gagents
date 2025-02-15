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
        return Task.FromResult(new JobProgressResult<ProgramCode>()
            { Result = new ProgramCode() { Content = "this is product design" } });
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

public interface IProgrammerGAgent : IJob<FrameworkDesign, ProgramCode>, IGAgent
{
}

[GenerateSerializer]
public class ProgramCode
{
    public string Content { get; set; }
}