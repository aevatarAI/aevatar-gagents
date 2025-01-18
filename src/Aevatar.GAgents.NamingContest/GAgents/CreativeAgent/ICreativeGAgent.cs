using Aevatar.Core.Abstractions;
using Aevatar.GAgents.MicroAI.GAgent;
using Aevatar.GAgents.MicroAI.Model;


namespace Aevatar.GAgent.NamingContest.CreativeAgent;

public interface ICreativeGAgent:IStateGAgent<CreativeState>
{
    // Task InitAgentsAsync(ContestantAgent contestantAgent);
    Task<string> GetCreativeNaming();

    Task<string> GetCreativeName();
}