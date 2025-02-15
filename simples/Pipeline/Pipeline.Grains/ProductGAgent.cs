using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Pipeline.Abstract;
using Microsoft.Extensions.Logging;

namespace Pipeline.Grains;

public class ProductGAgent : GAgentBase<ProductGAgentState,ProductGAgentLogEvent>,IProductGAgent
{
    public Task<JobProgressResult<ProductDesign>> ProcessAsync(ProductRequirements param)
    {
        return Task.FromResult(new JobProgressResult<ProductDesign>()
            { Result = new ProductDesign() { Content = "this is product design" } });
    }

    public ProductGAgent(ILogger<ProductGAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }
}

[GenerateSerializer]
public class ProductGAgentState : StateBase
{
    
}

[GenerateSerializer]
public class ProductGAgentLogEvent : StateLogEventBase<ProductGAgentLogEvent>
{
    
}

public interface IProductGAgent : IJob<ProductRequirements, ProductDesign>,IGAgent
{
}

[GenerateSerializer]
public class ProductRequirements
{
    public string Content { get; set; }
}

[GenerateSerializer]
public class ProductDesign
{
    public string Content { get; set; }
}