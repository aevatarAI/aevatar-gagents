using System.Threading.Tasks;
using Orleans;

namespace AevatarGAgents.NamingContest.VoteGAgent.Grains;

public interface IVoteAgentGrain :  IGrainWithGuidKey
{
    Task VoteAgentAsync(VoteCharmingEvent voteCharmingEvent);
    
    Task VoteAgentAsync(SingleVoteCharmingEvent singleVoteCharmingEvent);

}