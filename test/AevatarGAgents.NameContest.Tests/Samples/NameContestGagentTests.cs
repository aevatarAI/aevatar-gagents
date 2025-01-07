using System;
using System.Threading.Tasks;
using AevatarGAgents.NamingContest.Common;
using AevatarGAgents.NamingContest.Grains;
using AISmart.Agent;
using Orleans;
using Xunit;
using Xunit.Abstractions;

namespace AISmart.Samples
{
    public sealed class NameContestGagentTests : AISmartNameContestTestBase
    {
        private readonly IClusterClient _clusterClient;

        private INamingContestService _namingContestService;
        private readonly IPumpFunNamingContestGAgent _pumpFunNamingContestGAgent;
        private readonly INamingContestGrain _namingContestGrain;


        public NameContestGagentTests(ITestOutputHelper output)
        {
            _namingContestService = GetRequiredService<INamingContestService>();
            _clusterClient = GetRequiredService<IClusterClient>();
            _pumpFunNamingContestGAgent = _clusterClient.GetGrain<IPumpFunNamingContestGAgent>(Guid.NewGuid());
            _namingContestGrain = _clusterClient.GetGrain<INamingContestGrain>("NamingContestGrain");
        }

        public async Task InitializeAsync()
        {
        }

        public Task DisposeAsync()
        {
            // Clean up resources if needed
            return Task.CompletedTask;
        }

        [Fact]
        public async Task InitAgents_Test()
        {

            var namingLogEvent = new NamingLogEvent(NamingContestStepEnum.Complete, Guid.Empty);

            await _namingContestGrain.SendMessageAsync(Guid.NewGuid(),namingLogEvent as NamingLogEvent,"https://xxx.com");
        }
    }
}