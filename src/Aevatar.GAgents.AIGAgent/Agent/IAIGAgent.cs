using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AIGAgent.Dtos;

namespace Aevatar.GAgents.AIGAgent.Agent;

public interface IAIGAgent
{
    Task<bool> InitializeAsync(InitializeDto dto);

    Task<bool> UploadKnowledge(List<BrainContentDto>? knowledgeList);
}