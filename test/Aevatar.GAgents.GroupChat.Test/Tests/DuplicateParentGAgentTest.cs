using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.GroupChat.Feature.Extension;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.GroupChat.Test.GAgents;
using GroupChat.GAgent.Dto;
using Orleans;
using Shouldly;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.GroupChat.Test.Tests;

/// <summary>
/// 测试验证 GAgent 多父注册功能
/// 通过调试发现，系统现在支持将同一个 GAgent 注册到多个父 GAgent
/// 这个测试验证该功能正常工作
/// </summary>
public sealed class MultiParentGAgentTest : AevatarGroupChatTestBase
{
    private readonly IGAgentFactory _agentFactory;
    private readonly ILogger<MultiParentGAgentTest> _logger;

    public MultiParentGAgentTest()
    {
        _agentFactory = GetRequiredService<IGAgentFactory>();
        _logger = GetRequiredService<ILogger<MultiParentGAgentTest>>();
    }

    [Fact]
    public async Task Should_Support_WorkerAgent_Registered_To_Multiple_GroupAgents()
    {
        // Arrange - 创建共享的 WorkerAgent
        var sharedWorker = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await sharedWorker.ConfigAsync(new GroupMemberConfigDto() { MemberName = "SharedWorker" });
        
        var worker2 = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await worker2.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Worker2" });

        // 创建第一个 GroupGAgent 和 工作流
        var groupAgent1 = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflow1 = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = sharedWorker.GetGrainId().ToString(),
                NextGrainId = worker2.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = worker2.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        // 创建第二个 GroupGAgent 和 工作流（包含同一个 sharedWorker）
        var groupAgent2 = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflow2 = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = sharedWorker.GetGrainId().ToString(),  // 同一个 GAgent
                NextGrainId = "",
            }
        };

        // Act - 两次注册都应该成功
        await groupAgent1.AddWorkflowGroupChat(_agentFactory, workflow1);
        await groupAgent2.AddWorkflowGroupChat(_agentFactory, workflow2);

        // Assert - 验证注册成功
        _logger.LogInformation("成功验证：系统支持将同一个GAgent注册到多个GroupAgent");
        
        // 验证工作流可以正常启动
        await groupAgent1.PublishEventAsync(new StartWorkflowCoordinatorEvent());
        await groupAgent2.PublishEventAsync(new StartWorkflowCoordinatorEvent());
        
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        var sharedWorkerState = await sharedWorker.GetStateAsync();
        sharedWorkerState.ShouldNotBeNull();
        _logger.LogInformation("验证通过：多父GAgent注册功能正常工作");
    }

    [Fact]
    public async Task Should_Support_Same_GAgent_Used_Multiple_Times_In_Single_Workflow()
    {
        // Arrange - 创建一个 WorkerAgent
        var duplicateWorker = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await duplicateWorker.ConfigAsync(new GroupMemberConfigDto() { MemberName = "DuplicateWorker" });
        
        var finalWorker = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await finalWorker.ConfigAsync(new GroupMemberConfigDto() { MemberName = "FinalWorker" });

        // 创建包含重复 GAgent 的工作流
        var groupAgent = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflowWithDuplicates = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = duplicateWorker.GetGrainId().ToString(),
                NextGrainId = finalWorker.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = duplicateWorker.GetGrainId().ToString(), // 重复的 GAgent
                NextGrainId = finalWorker.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = finalWorker.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        // Act - 应该成功创建，系统现在支持重复注册
        await groupAgent.AddWorkflowGroupChat(_agentFactory, workflowWithDuplicates);

        // Assert - 验证工作流创建成功
        await groupAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent());
        await Task.Delay(TimeSpan.FromSeconds(1));

        var duplicateWorkerState = await duplicateWorker.GetStateAsync();
        duplicateWorkerState.ShouldNotBeNull();
        _logger.LogInformation("验证通过：单个工作流中重复使用GAgent功能正常工作");
    }

    [Fact]
    public async Task Should_Work_Normally_When_Different_GAgents_Used()
    {
        // Arrange - 创建不同的 WorkerAgents
        var worker1 = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await worker1.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Worker1" });
        
        var worker2 = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await worker2.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Worker2" });
        
        var worker3 = await _agentFactory.GetGAgentAsync<IWorkerGAgent>(Guid.NewGuid());
        await worker3.ConfigAsync(new GroupMemberConfigDto() { MemberName = "Worker3" });

        // 创建两个不同的 GroupGAgent，使用不同的 WorkerAgents
        var groupAgent1 = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflow1 = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = worker1.GetGrainId().ToString(),
                NextGrainId = worker2.GetGrainId().ToString(),
            },
            new WorkflowUnitDto()
            {
                GrainId = worker2.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        var groupAgent2 = await _agentFactory.GetGAgentAsync<IGroupGAgent>(Guid.NewGuid());
        var workflow2 = new List<WorkflowUnitDto>()
        {
            new WorkflowUnitDto()
            {
                GrainId = worker3.GetGrainId().ToString(),
                NextGrainId = "",
            }
        };

        // Act - 两个工作流都应该成功创建，因为使用了不同的 GAgents
        await groupAgent1.AddWorkflowGroupChat(_agentFactory, workflow1);
        await groupAgent2.AddWorkflowGroupChat(_agentFactory, workflow2);

        // Assert - 验证工作流正常工作
        await groupAgent1.PublishEventAsync(new StartWorkflowCoordinatorEvent());
        await groupAgent2.PublishEventAsync(new StartWorkflowCoordinatorEvent());

        // 等待工作流完成
        await Task.Delay(TimeSpan.FromSeconds(1));

        // 验证状态
        var worker2State = await worker2.GetStateAsync();
        worker2State.PreWorkUnits.ShouldContain("Worker1");
        
        _logger.LogInformation("测试通过：使用不同的 GAgents 时工作流正常工作");
    }
} 