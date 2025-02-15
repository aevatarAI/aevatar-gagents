using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Pipeline.Abstract;
using Microsoft.Extensions.Logging;

namespace Pipeline.Grains;

public class DesignerGAgent : GAgentBase<DesignerGAgentState, DesignerGAgentLogEvent>, IDesignerGAgent
{
    public DesignerGAgent(ILogger<DesignerGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    public Task<JobProgressResult<FrameworkDesign>> ProcessAsync(ProductDesign param)
    {
        Console.WriteLine("this is framework design");
        return Task.FromResult(new JobProgressResult<FrameworkDesign>()
            { Result = new FrameworkDesign() { Content = "this is framework design" } });
    }
}

[GenerateSerializer]
public class DesignerGAgentState : StateBase
{
}

[GenerateSerializer]
public class DesignerGAgentLogEvent : StateLogEventBase<DesignerGAgentLogEvent>
{
}

public interface IDesignerGAgent : IJob<ProductDesign, FrameworkDesign>
{
}

[GenerateSerializer]
public class FrameworkDesign
{
    public string Content { get; set; }
}