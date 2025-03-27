using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using GroupChat.GAgent.Dto;
using Orleans;
using Shouldly;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

public sealed class GroupChatWorkflowTest : AevatarGroupChatTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public GroupChatWorkflowTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task WorkflowTest()
    {
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni"});
        
        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom"});
        
        var jeni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await jeni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Jeni"});
        
        var carber = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await carber.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Carber"});
        
        var fread = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await fread.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Fread"});
        
        var moni = await _agentFactory.GetGAgentAsync<ILeaderGAgent>(Guid.NewGuid());
        await moni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Moni"});
        
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflows = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = toni.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = tom.GetGrainId().ToString(),
                NextGrainId = jeni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = jeni.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = carber.GetGrainId().ToString(),
                NextGrainId = fread.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = fread.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = moni.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };
        
        await groupAgent.AddWorkflowGroupChat(_agentFactory, workflows);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        // wait workflow run complate
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        var jeniState = await jeni.GetStateAsync();
        jeniState.PreWorkUnits.Count.ShouldBe(2);
        jeniState.PreWorkUnits.ShouldContain("Toni");
        jeniState.PreWorkUnits.ShouldContain("Tom");
        
        var freadState = await fread.GetStateAsync();
        freadState.PreWorkUnits.Count.ShouldBe(2);
        freadState.PreWorkUnits.ShouldContain("Jeni");
        freadState.PreWorkUnits.ShouldContain("Carber");
        
        var moniState = await moni.GetStateAsync();
        moniState.AgentNames.Count.ShouldBe(1);
        moniState.AgentNames.ShouldContain("Fread");
    }
}