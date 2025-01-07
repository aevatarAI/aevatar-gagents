using System.Threading.Tasks;
using AevatarGAgents.AElf.Dto;
using Orleans;

namespace AevatarGAgents.AElf.Agent.Grains;

public interface ITransactionGrain:IGrainWithGuidKey
{
    Task <TransactionDto> SendAElfTransactionAsync(SendTransactionDto sendTransactionDto);
    Task <TransactionDto> LoadAElfTransactionResultAsync(QueryTransactionDto queryTransactionDto);
    
    Task <TransactionDto> GetAElfTransactionAsync(QueryTransactionDto queryTransactionDto);
}