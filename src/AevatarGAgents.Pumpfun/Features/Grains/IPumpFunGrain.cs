using System.Threading.Tasks;
using Orleans;

namespace AevatarGAgents.PumpFun.Grains;

public interface IPumpFunGrain : IGrainWithGuidKey
{
    public Task SendMessageAsync(string replyId, string? replyMessage);
   
}