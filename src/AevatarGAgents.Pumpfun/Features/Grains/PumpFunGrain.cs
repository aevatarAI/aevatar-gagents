using System.Threading.Tasks;
using AevatarGAgents.PumpFun.Provider;
using Orleans;
using Orleans.Providers;

namespace AevatarGAgents.PumpFun.Grains;

[StorageProvider(ProviderName = "PubSubStore")]
public class PumpFunGrain : Grain<PumpFunState>, IPumpFunGrain
{
    public readonly IPumpFunProvider PumpFunProvider;
    
    public PumpFunGrain(IPumpFunProvider pumpFunProvider) 
    {
        PumpFunProvider = pumpFunProvider;
    }

    public async Task SendMessageAsync(string replyId, string? replyMessage)
    {
        if (replyMessage != null)
        {
            await PumpFunProvider.SendMessageAsync(replyId, replyMessage);
        }
    }
}