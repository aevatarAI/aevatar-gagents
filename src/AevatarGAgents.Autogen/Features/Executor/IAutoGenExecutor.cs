using System.Threading.Tasks;
using Orleans;

namespace AevatarGAgents.Autogen.Executor;

public interface IAutoGenExecutor : IGrainWithGuidKey
{
    // Task ExecuteTaskAsync(Guid taskId, List<IMessage> history);
    Task ExecuteTaskAsync(ExecutorTaskInfo taskInfo);
}