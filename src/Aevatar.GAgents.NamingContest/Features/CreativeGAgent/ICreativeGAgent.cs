using System.Threading.Tasks;
using Aevatar.GAgents.MicroAI.Agent;

namespace Aevatar.GAgents.NamingContest.CreativeGAgent;

public interface ICreativeGAgent:IMicroAIGAgent
{
    // Task InitAgentsAsync(ContestantAgent contestantAgent);
    Task<string> GetCreativeNaming();

    Task<string> GetCreativeName();

}