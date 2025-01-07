using System.Threading.Tasks;

namespace AevatarGAgents.PumpFun.Provider;

public interface IPumpFunProvider
{
    public Task SendMessageAsync(string replyId, string replyMessage);
}