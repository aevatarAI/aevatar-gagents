using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Orleans;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AISmart.Samples
{
    public sealed class NameContestTests : AISmartNameContestTestBase
    {
        private readonly INamingContestService _namingContestService;
        private readonly IClusterClient _clusterClient;


        public NameContestTests(ITestOutputHelper output)
        {
            _namingContestService = GetRequiredService<INamingContestService>();
            _clusterClient = GetRequiredService<IClusterClient>();
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
            ContestAgentsDto contestAgentsDto = new ContestAgentsDto()
            {
                Network = new List<CommonAgent>()
                {
                    new CommonAgent()
                    {
                        Name = "james",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kob",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },

                    new CommonAgent()
                    {
                        Name = "james",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kob",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                },
            };
            AiSmartInitResponse agentResponse = await _namingContestService.InitAgentsAsync(contestAgentsDto);

            agentResponse.Details.Count.ShouldBe(4);
            agentResponse.Details.FirstOrDefault()!.AgentName.ShouldBe("james");
            agentResponse.Details[1].AgentName.ShouldBe("kob");
        }

        [Fact]
        public async Task InitNetworks_Test()
        {
            ContestAgentsDto contestAgentsDto = new ContestAgentsDto()
            {
                Network = new List<CommonAgent>()
                {
                    new CommonAgent()
                    {
                        Name = "james",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kob",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },

                    new CommonAgent()
                    {
                        Name = "james",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kob",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                },
            };
            AiSmartInitResponse aiSmartInitResponse = await _namingContestService.InitAgentsAsync(contestAgentsDto);

            aiSmartInitResponse.Details.Count.ShouldBe(4);
            aiSmartInitResponse.Details.FirstOrDefault()!.AgentName.ShouldBe("james");
            aiSmartInitResponse.Details[1].AgentName.ShouldBe("kob");


            NetworksDto networksDto = new NetworksDto()
            {
                Networks = new List<Network>()
                {
                    new Network()
                    {
                        ConstentList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelContestant)
                            .Select(agent => agent.AgentId).ToList(),
                        JudgeList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelJudge)
                            .Select(agent => agent.AgentId).ToList(),
                        ScoreList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelJudge)
                            .Select(agent => agent.AgentId).ToList(),
                        HostList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelHost)
                            .Select(agent => agent.AgentId).ToList(),
                        // HostList = agentResponse.ContestantAgentList.Select(agent => agent.AgentId).ToList(),
                        Name = "FirstRound-1",
                        Round = "1",
                        CallbackAddress = "https://xxxx.com"
                    }
                }
            };
            GroupResponse groupResponse = await _namingContestService.InitNetworksAsync(networksDto);

            groupResponse.GroupDetails.Count.ShouldBe(1);
            groupResponse.GroupDetails.FirstOrDefault()!.Name.ShouldBe("FirstRound-1");
            groupResponse.GroupDetails.FirstOrDefault()!.GroupId.ShouldNotBeNull();
        }

        [Fact]
        public async Task Start_Group_Test()
        {
            ContestAgentsDto contestAgentsDto = new ContestAgentsDto()
            {
                Network = new List<CommonAgent>()
                {
                    new CommonAgent()
                    {
                        Name = "james",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kob",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },

                    new CommonAgent()
                    {
                        Name = "jamesJudge",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kobJudge",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    
                    new CommonAgent()
                    {
                        Name = "jamesHost",
                        Label = "Host",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kobHost",
                        Label = "Host",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                },
            };
            AiSmartInitResponse aiSmartInitResponse = await _namingContestService.InitAgentsAsync(contestAgentsDto);

            aiSmartInitResponse.Details.Count.ShouldBe(6);
            aiSmartInitResponse.Details.FirstOrDefault()!.AgentName.ShouldBe("james");
            aiSmartInitResponse.Details[1].AgentName.ShouldBe("kob");


            NetworksDto networksDto = new NetworksDto()
            {
                Networks = new List<Network>()
                {
                    new Network()
                    {
                        ConstentList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelContestant)
                            .Select(agent => agent.AgentId).ToList(),
                        JudgeList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelJudge)
                            .Select(agent => agent.AgentId).ToList(),
                        ScoreList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelJudge)
                            .Select(agent => agent.AgentId).ToList(),
                        HostList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelHost)
                            .Select(agent => agent.AgentId).ToList(),
                        // HostList = agentResponse.ContestantAgentList.Select(agent => agent.AgentId).ToList(),
                        Name = "FirstRound-1",
                        Round = "1",
                        CallbackAddress = "https://xxxx.com"
                    }
                }
            };
            GroupResponse groupResponse = await _namingContestService.InitNetworksAsync(networksDto);

            groupResponse.GroupDetails.Count.ShouldBe(1);
            groupResponse.GroupDetails.FirstOrDefault()!.Name.ShouldBe("FirstRound-1");
            groupResponse.GroupDetails.FirstOrDefault()!.GroupId.ShouldNotBeNull();


            GroupStartDto groupStartDto = new GroupStartDto()
            {
                GroupIdList = new List<string>()
                {
                    groupResponse.GroupDetails.FirstOrDefault()!.GroupId
                }
            };

            await _namingContestService.StartGroupAsync(groupStartDto);

            await Task.Delay(1000 * 200);
        }


        [Fact]
        public async Task Init_Most_Charming_Network_Test()
        {
            ContestAgentsDto contestAgentsDto = new ContestAgentsDto()
            {
                Network = new List<CommonAgent>()
                {
                    new CommonAgent()
                    {
                        Name = "james",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kob",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },

                    new CommonAgent()
                    {
                        Name = "jamesJudge",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kobJudge",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                },
            };
            AiSmartInitResponse aiSmartInitResponse = await _namingContestService.InitAgentsAsync(contestAgentsDto);

            aiSmartInitResponse.Details.Count.ShouldBe(4);
            aiSmartInitResponse.Details.FirstOrDefault()!.AgentName.ShouldBe("james");
            aiSmartInitResponse.Details[1].AgentName.ShouldBe("kob");


            NetworksDto networksDto = new NetworksDto()
            {
                Networks = new List<Network>()
                {
                    new Network()
                    {
                        ConstentList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelContestant)
                            .Select(agent => agent.AgentId).ToList(),
                        JudgeList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelJudge)
                            .Select(agent => agent.AgentId).ToList(),
                        ScoreList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelJudge)
                            .Select(agent => agent.AgentId).ToList(),
                        HostList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelHost)
                            .Select(agent => agent.AgentId).ToList(),
                        // HostList = agentResponse.ContestantAgentList.Select(agent => agent.AgentId).ToList(),
                        Name = "FirstRound-1",
                        Round = "1",
                        CallbackAddress = "https://xxxx.com"
                    }
                }
            };
            GroupResponse groupResponse = await _namingContestService.InitNetworksAsync(networksDto);

            groupResponse.GroupDetails.Count.ShouldBe(1);
            groupResponse.GroupDetails.FirstOrDefault()!.Name.ShouldBe("FirstRound-1");
            groupResponse.GroupDetails.FirstOrDefault()!.GroupId.ShouldNotBeNull();

            IVoteCharmingGAgent voteCharmingGAgent =
                _clusterClient.GetGrain<IVoteCharmingGAgent>(GuidUtil.StringToGuid("AI-Most-Charming-Naming-Contest"));

            // todo voteCharmingGAgent unit Test 
        }


        [Fact]
        public async Task Most_Charming_Agent_Test()
        {
            ContestAgentsDto contestAgentsDto = new ContestAgentsDto()
            {
                Network = new List<CommonAgent>()
                {
                    new CommonAgent()
                    {
                        Name = "james",
                        Label = "Contestant",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    // new CommonAgent()
                    // {
                    //     Name = "kob",
                    //     Label = "Contestant",
                    //     Bio = JsonSerializer.Serialize(new
                    //     {
                    //         Description =
                    //             "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                    //     }),
                    // },

                    new CommonAgent()
                    {
                        Name = "jamesJudge",
                        Label = "Judge",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    // new CommonAgent()
                    // {
                    //     Name = "kobJudge",
                    //     Label = "Judge",
                    //     Bio = JsonSerializer.Serialize(new
                    //     {
                    //         Description =
                    //             "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                    //     }),
                    // },
                    new CommonAgent()
                    {
                        Name = "jamesHost",
                        Label = "Host",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "James is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                    new CommonAgent()
                    {
                        Name = "kobHost",
                        Label = "Host",
                        Bio = JsonSerializer.Serialize(new
                        {
                            Description =
                                "kob is a renowned NBA superstar known for his exceptional skills on the basketball court, his leadership abilities, and his contributions to the game. With a career spanning over multiple years, he has won numerous awards, including MVP titles and championship rings. Off the court, James is admired for his philanthropy, community involvement, and dedication to inspiring the next generation of athletes."
                        }),
                    },
                },
            };
            AiSmartInitResponse aiSmartInitResponse = await _namingContestService.InitAgentsAsync(contestAgentsDto);

            aiSmartInitResponse.Details.Count.ShouldBe(4);
            aiSmartInitResponse.Details.FirstOrDefault()!.AgentName.ShouldBe("james");
            // aiSmartInitResponse.Details[1].AgentName.ShouldBe("kob");


            NetworksDto networksDto = new NetworksDto()
            {
                Networks = new List<Network>()
                {
                    new Network()
                    {
                        ConstentList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelContestant)
                            .Select(agent => agent.AgentId).ToList(),
                        JudgeList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelJudge)
                            .Select(agent => agent.AgentId).ToList(),
                        ScoreList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelJudge)
                            .Select(agent => agent.AgentId).ToList(),
                        HostList = aiSmartInitResponse.Details
                            .FindAll(agent => agent.Label == NamingContestConstant.AgentLabelHost)
                            .Select(agent => agent.AgentId).ToList(),
                        // HostList = agentResponse.ContestantAgentList.Select(agent => agent.AgentId).ToList(),
                        Name = "FirstRound-1",
                        Round = "1",
                        CallbackAddress = "https://xxxx.com"
                    }
                }
            };
            GroupResponse groupResponse = await _namingContestService.InitNetworksAsync(networksDto);

            groupResponse.GroupDetails.Count.ShouldBe(1);
            groupResponse.GroupDetails.FirstOrDefault()!.Name.ShouldBe("FirstRound-1");
            groupResponse.GroupDetails.FirstOrDefault()!.GroupId.ShouldNotBeNull();


            GroupStartDto groupStartDto = new GroupStartDto()
            {
                GroupIdList = new List<string>()
                {
                    groupResponse.GroupDetails.FirstOrDefault()!.GroupId
                }
            };

            await _namingContestService.StartGroupAsync(groupStartDto);

            await Task.Delay(1000 * 600);
        }


        [Fact]
        public async Task Init_Multi_Agents_With_Bio_Test()
        {
            var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Samples", "NA101-200.json");

            var contestantAgentList = LoadConfiguration(jsonFilePath);

            ContestAgentsDto contestAgentsDto = new ContestAgentsDto()
            {
                // ContestantAgentList = contestantAgentList,
                // JudgeAgentList = new List<JudgeAgent>()
                // {
                //     new JudgeAgent()
                //     {
                //         Name = "james",
                //     },
                //     new JudgeAgent()
                //     {
                //         Name = "kob",
                //     },
                // },
                // HostAgentList = new List<HostAgent>()
                // {
                // }
            };
            AiSmartInitResponse agentResponse = await _namingContestService.InitAgentsAsync(contestAgentsDto);

            agentResponse.Details.Count.ShouldBe(4);
            agentResponse.Details.FirstOrDefault()!.AgentName.ShouldBe("james");
            agentResponse.Details[1].AgentName.ShouldBe("kob");


            var agentId = agentResponse.Details.FirstOrDefault()!.AgentId;
            var creativeGAgent = _clusterClient.GetGrain<ICreativeGAgent>(Guid.Parse(agentId));
            var state = creativeGAgent.GetAgentState();
            state.Result.AgentResponsibility.ShouldBe(contestantAgentList.FirstOrDefault()!.Bio);
            state.Result.AgentName.ShouldBe(contestantAgentList.FirstOrDefault()!.Name);

            agentResponse.Details[1].AgentName.ShouldBe("kob");
        }


        private static List<ContestantAgent>? LoadConfiguration(string jsonFilePath)
        {
            // Read the entire file into a string
            string jsonString = File.ReadAllText(jsonFilePath);

            // Assume the JSON array is an array of objects
            // Replace MyClass with the appropriate class for your JSON structure
            List<ContestantAgent>? items = JsonSerializer.Deserialize<List<ContestantAgent>>(jsonString);

            return items;
        }
    }
}