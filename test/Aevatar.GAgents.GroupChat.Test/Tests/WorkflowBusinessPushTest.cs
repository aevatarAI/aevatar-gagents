using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using GroupChat.GAgent.Dto;
using Shouldly;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

public sealed class WorkflowBusinessPushTest : AevatarGroupChatTestBase
{
    private readonly IGAgentFactory _agentFactory;

    public WorkflowBusinessPushTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task WorkflowCompletionWithBusinessEventAndFinalResultsTest()
    {
        // Setup business event listener to capture WorkflowCompletionBusinessPushEvent
        var businessListener = await _agentFactory.GetGAgentAsync<IBusinessEventListenerGAgent>(Guid.NewGuid());
        await businessListener.ConfigAsync(new BusinessEventListenerConfigDto { Name = "TestListener" });

        // Reuse basic setup from GroupChatWorkflowTest, focus on verifying business event publishing
        var toni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await toni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Toni"});
        
        var tom = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await tom.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Tom"});
        
        var jeni = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await jeni.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Jeni"});
        
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
                GrainId = fread.GetGrainId().ToString(),
                NextGrainId = moni.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = moni.GetGrainId().ToString(),
                NextGrainId = "", // Moni is the final node
            }
        };
        
        await groupAgent.AddWorkflowGroupChat(_agentFactory, workflows);
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent() { });

        // wait workflow run complete
        await Task.Delay(TimeSpan.FromSeconds(2));
        
        // Verify original workflow functionality works normally - proves our changes don't break existing features
        var jeniState = await jeni.GetStateAsync();
        jeniState.PreWorkUnits.Count.ShouldBe(2);
        jeniState.PreWorkUnits.ShouldContain("Toni");
        jeniState.PreWorkUnits.ShouldContain("Tom");
        
        var freadState = await fread.GetStateAsync();
        freadState.PreWorkUnits.Count.ShouldBe(1);
        freadState.PreWorkUnits.ShouldContain("Jeni");
        
        var moniState = await moni.GetStateAsync();
        moniState.AgentNames.Count.ShouldBe(1);
        moniState.AgentNames.ShouldContain("Fread");
        
        // NEW: Verify business event listener received WorkflowCompletionBusinessPushEvent with final results
        var listenerState = await businessListener.GetStateAsync();
        listenerState.ReceivedEvents.ShouldNotBeEmpty("Business event listener should have received events");
        
        var completionEvents = listenerState.ReceivedEvents.OfType<WorkflowCompletionBusinessPushEvent>().ToList();
        completionEvents.Count.ShouldBe(1, "Should receive exactly one workflow completion event");
        
        var completionEvent = completionEvents.First();
        completionEvent.WorkflowId.ShouldNotBeNullOrEmpty("WorkflowId should be set");
        completionEvent.BlackboardId.ShouldNotBe(Guid.Empty, "BlackboardId should be set");
        completionEvent.CompletionTime.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1), "CompletionTime should be recent");
        
        // KEY IMPROVEMENT: Verify FinalResult is now included (simplified structure)
        completionEvent.FinalResult.ShouldNotBeNull("FinalResult should contain the actual result from final node");
        completionEvent.FinalResult.Content.ShouldNotBeNullOrEmpty("Final result should have content");
        
        // Main verification: Workflow completes normally with ACTUAL final result
        // Business systems can now receive the real ChatResponse from the final workflow node
        // In actual usage, business systems get the meaningful business result, not just statistics
    }
} 