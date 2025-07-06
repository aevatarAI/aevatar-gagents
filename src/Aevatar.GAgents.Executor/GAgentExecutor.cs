using Aevatar.Core;
using Aevatar.Core.Abstractions;
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
            
            // Publish the event directly to the target GAgent 
            // The target GAgent will process it and publish any result events
            await gAgent.PublishAsync(@event);

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