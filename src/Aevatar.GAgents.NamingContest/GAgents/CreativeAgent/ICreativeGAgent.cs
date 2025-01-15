using Aevatar.GAgents.MicroAI.GAgent;


namespace Aevatar.GAgent.NamingContest.CreativeAgent;

public interface ICreativeGAgent:IMicroAIGAgent
{
    // Task InitAgentsAsync(ContestantAgent contestantAgent);
    Task<string> GetCreativeNaming();

    Task<string> GetCreativeName();

    Task<int> GetExecuteStep();
}