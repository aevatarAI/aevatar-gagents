using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Brain;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.AIGAgent.Agent;

public interface IAIGAgent
{
    Task<bool> InitializeAsync(InitializeDto dto);

    Task<bool> UploadKnowledgeAsync(List<BrainContentDto>? knowledgeList);

    Task<IBrain?> GetBrainAsync();
}