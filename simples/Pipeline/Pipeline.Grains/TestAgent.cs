using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Pipeline.Grains;

public class TestAgent: GAgentBase<TestAgentState,TestAgentLogEvent>,ITest
{
    public TestAgent(ILogger<TestAgent> logger) : base(logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        throw new NotImplementedException();
    }

    public Task Input()
    {
        throw new NotImplementedException();
    }
}


[GenerateSerializer]
public class TestAgentState : StateBase
{
    
}

[GenerateSerializer]
public class TestAgentLogEvent : StateLogEventBase<TestAgentLogEvent>
{
    
}

public interface ITest : IGAgent
{
    Task Input();
}