using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.PublishGAgent;
using Orleans.Streams;

namespace Aevatar.GAgents.Executor;

// Event to inform execution result.
[GenerateSerializer]
public class ExecutionCompletedEvent
{
    [Id(0)] public string ExecutionId { get; set; } = string.Empty;
    [Id(1)] public string Result { get; set; } = string.Empty;
}

public class GAgentExecutor : IGAgentExecutor
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IClusterClient _clusterClient;

    public GAgentExecutor(IClusterClient clusterClient)
    {
        _gAgentFactory = new GAgentFactory(clusterClient);
        _clusterClient = clusterClient;
    }

    public async Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event)
    {
        var resultGAgent = await _gAgentFactory.GetGAgentAsync<IResultGAgent>();
        var publishingGAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>();

        var executionId = Guid.NewGuid().ToString();

        var streamProvider = _clusterClient.GetStreamProvider(AevatarCoreConstants.StreamProvider);
        var resultStream =
            streamProvider.GetStream<ExecutionCompletedEvent>(
                AevatarGAgentExecutorConstants.GAgentExecutorStreamNamespace, executionId);

        var resultTask = new TaskCompletionSource<string>();
        var subscription = await resultStream.SubscribeAsync((result, token) =>
        {
            resultTask.SetResult(result.Result);
            return Task.CompletedTask;
        });

        try
        {
            await resultGAgent.SetExecutionContextAsync(executionId, AevatarCoreConstants.StreamProvider,
                AevatarGAgentExecutorConstants.GAgentExecutorStreamNamespace);

            // Subscribe ResultGAgent to the target GAgent to receive results
            await gAgent.RegisterAsync(resultGAgent);
            
            // Also subscribe the target GAgent to PublishingGAgent to receive the event
            await publishingGAgent.RegisterAsync(gAgent);
            
            // Publish the event through PublishingGAgent 
            // The target GAgent will receive and process it, then publish any result events
            await publishingGAgent.PublishEventAsync(@event);

            return await resultTask.Task.WaitAsync(AevatarGAgentExecutorConstants.GAgentExecutorTimeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"ExecuteGAgentEventHandler timeout for execution {executionId}");
        }
        finally
        {
            await subscription.UnsubscribeAsync();
            // Unregister ResultGAgent from the target GAgent
            await gAgent.UnregisterAsync(resultGAgent);
            // Unregister target GAgent from PublishingGAgent
            await publishingGAgent.UnregisterAsync(gAgent);
        }
    }

    public async Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event)
    {
        var targetGAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        return await ExecuteGAgentEventHandler(targetGAgent, @event);
    }

    public async Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event)
    {
        var grainId = GrainId.Create(grainType, Guid.NewGuid().ToString());
        return await ExecuteGAgentEventHandler(grainId, @event);
    }
}