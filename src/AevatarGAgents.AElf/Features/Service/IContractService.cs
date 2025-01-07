using System.Threading.Tasks;
using AElf.Contracts.MultiToken;

namespace AevatarGAgents.AElf.Service;

public interface IContractService
{
  
    public Task<string>  SendTransferAsync(string chainId, string senderName, TransferInput transferInput);
}