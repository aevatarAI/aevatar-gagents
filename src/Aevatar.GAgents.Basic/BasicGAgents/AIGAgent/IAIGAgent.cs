using System.Threading.Tasks;
using Aevatar.GAgents.Basic.Dtos;

namespace Aevatar.GAgents.Basic;

public interface IAIGAgent
{
    Task<bool> InitializeAsync(InitializeDto dto);
}