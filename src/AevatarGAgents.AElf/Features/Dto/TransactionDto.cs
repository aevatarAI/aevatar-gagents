using Orleans;

namespace AevatarGAgents.AElf.Dto;

[GenerateSerializer]
public class TransactionDto
{
    [Id(0)] public bool IsSuccess  { get; set; }
    
    [Id(1)] public string TransactionId { get; set; }
}