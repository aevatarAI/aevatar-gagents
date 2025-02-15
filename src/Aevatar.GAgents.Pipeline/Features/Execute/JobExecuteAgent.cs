using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Pipeline.Abstract;
using Aevatar.GAgents.Pipeline.Features.Common;
using Microsoft.Extensions.Logging;
using Orleans.Streams;

namespace Aevatar.GAgents.Pipeline.Features.ExecuteAgent;

internal class JobExecuteAgent : Grain, IExecuteJob
{
    private IStreamProvider StreamProvider => this.GetStreamProvider(AevatarCoreConstants.StreamProvider);
    private readonly ILogger<JobExecuteAgent> _logger;

    public JobExecuteAgent(ILogger<JobExecuteAgent> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteJobAsync(JobExecuteInfo jobExecuteInfo)
    {
        bool ifContinue = false;
        object? response = null;
        try
        {
            var interfaces = GetDirectlyInheritedInterfaces(jobExecuteInfo.JobType).ToList();
            if (interfaces.Count() == 1)
            {
                var stepGrain = GrainFactory.GetGrain(interfaces[0], jobExecuteInfo.JobId);
                if (stepGrain != null)
                {
                    var jobType = interfaces[0].GetInterfaces().FirstOrDefault(f =>
                        f.IsGenericType && f.GetGenericTypeDefinition() == typeof(IJob<,>));
                    if (jobType != null)
                    {
                        var processMethod = jobType.GetMethod("ProcessAsync");
                        if (processMethod != null)
                        {
                            var executeTask = processMethod.Invoke(stepGrain,
                                new[] { jobExecuteInfo.JobInputParam });
                            if (executeTask is Task task)
                            {
                                await task;

                                var taskResponse = task.GetType().GetProperty("Result");
                                var result = taskResponse?.GetValue(task);
                                if (result != null)
                                {
                                    var responseType = result.GetType();
                                    var ifContinueProperty = responseType.GetProperty("IfContinue");
                                    var resultProperty = responseType.GetProperty("Result");
                                    ifContinue = (bool)ifContinueProperty?.GetValue(result);
                                    response = resultProperty?.GetValue(result);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Pipeline] JobExecuteAgent ExecuteJobAsync error");
        }

        await PublishInternalEvent(new JobExecuteResultInfo()
            { JobId = jobExecuteInfo.JobId, IfContinue = ifContinue, ExecuteResponse = response });
    }

    private async Task PublishInternalEvent(JobExecuteResultInfo publishData)
    {
        var streamId = StreamId.Create(CommonConstants.SteamProvider, this.GetPrimaryKey());
        var stream = StreamProvider.GetStream<JobExecuteBase>(streamId);
        await stream.OnNextAsync(publishData);
    }

    private IEnumerable<Type> GetDirectlyInheritedInterfaces(Type interfaceType)
    {
        var allInterfaces = new HashSet<Type>(interfaceType.GetInterfaces());
        var baseType = interfaceType.BaseType;
        if (baseType != null)
        {
            var baseTypeInterface = baseType.GetInterfaces();
            allInterfaces.RemoveAll(f => baseTypeInterface.Contains(f));
        }

        foreach (var iface in interfaceType.GetInterfaces())
        {
            var inheritedInterfacesFromParent = iface.GetInterfaces();
            foreach (var inheritedInterface in inheritedInterfacesFromParent)
            {
                allInterfaces.Remove(inheritedInterface);
            }
        }

        return allInterfaces;
    }
}

internal interface IExecuteJob : IGrainWithGuidKey
{
    Task ExecuteJobAsync(JobExecuteInfo jobExecuteInfo);
}