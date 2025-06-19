using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.Logging;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CustomConfigAIAgent.Test
{
    // 1. 自定义配置类
    [GenerateSerializer]
    public class MyCustomConfig : ConfigurationBase
    {
        // 基础AI配置
        [Id(0)] public string Instructions { get; set; } = string.Empty;
        [Id(1)] public LLMConfigDto LLMConfig { get; set; }
        [Id(2)] public bool StreamingModeEnabled { get; set; } = false;
        [Id(3)] public StreamingConfig StreamingConfig { get; set; }
        
        // 自定义业务配置参数
        [Id(4)] public string BusinessDomain { get; set; } = string.Empty;
        [Id(5)] public int MaxRetryAttempts { get; set; } = 3;
        [Id(6)] public TimeSpan TimeoutDuration { get; set; } = TimeSpan.FromMinutes(5);
        [Id(7)] public List<string> AllowedOperations { get; set; } = new List<string>();
        [Id(8)] public Dictionary<string, string> CustomSettings { get; set; } = new Dictionary<string, string>();
        [Id(9)] public bool EnableAdvancedFeatures { get; set; } = false;
        [Id(10)] public string WorkflowTemplate { get; set; } = string.Empty;
        [Id(11)] public int PriorityLevel { get; set; } = 1;
    }

    // 2. 自定义状态类
    [GenerateSerializer]
    public class MyCustomAIAgentState : AIGAgentStateBase
    {
        [Id(0)] public string BusinessDomain { get; set; } = string.Empty;
        [Id(1)] public int CurrentRetryCount { get; set; } = 0;
        [Id(2)] public DateTime LastOperationTime { get; set; } = DateTime.UtcNow;
        [Id(3)] public List<string> ExecutedOperations { get; set; } = new List<string>();
        [Id(4)] public Dictionary<string, object> OperationResults { get; set; } = new Dictionary<string, object>();
        [Id(5)] public bool IsAdvancedModeActive { get; set; } = false;
        [Id(6)] public int PriorityLevel { get; set; } = 1;
        [Id(7)] public List<string> AllowedOperations { get; set; } = new List<string>();
        [Id(8)] public Dictionary<string, string> CustomSettings { get; set; } = new Dictionary<string, string>();
    }

    // 3. 自定义状态日志事件类
    [GenerateSerializer]
    public class MyCustomAIAgentLogEvent : StateLogEventBase<MyCustomAIAgentLogEvent>
    {
        [Id(0)] public string Operation { get; set; } = string.Empty;
        [Id(1)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        [Id(2)] public bool Success { get; set; } = true;
        [Id(3)] public string ErrorMessage { get; set; } = string.Empty;
        [Id(4)] public Dictionary<string, object> OperationData { get; set; } = new Dictionary<string, object>();
    }

    // 4. 自定义事件类
    [GenerateSerializer]
    public class CustomBusinessEvent : EventBase
    {
        [Id(0)] public string Operation { get; set; } = string.Empty;
        [Id(1)] public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        [Id(2)] public int Priority { get; set; } = 1;
    }

    // 5. AI Agent接口
    public interface IMyCustomConfigAIAgent : IAIGAgent, IStateGAgent<MyCustomAIAgentState>
    {
        Task<string?> ProcessBusinessRequestAsync(string request, Dictionary<string, object>? parameters = null);
        Task<bool> ExecuteOperationAsync(string operation, Dictionary<string, object> parameters);
        Task<Dictionary<string, object>> GetOperationResultsAsync();
        Task<bool> ConfigureAdvancedFeaturesAsync(bool enable);
        Task<string?> ChatAsync(string message);
    }

    // 6. AI Agent实现
    [GAgent]
    public class MyCustomConfigAIAgent : 
        AIGAgentBase<MyCustomAIAgentState, MyCustomAIAgentLogEvent, CustomBusinessEvent, MyCustomConfig>, 
        IMyCustomConfigAIAgent
    {
        public MyCustomConfigAIAgent(ILogger<MyCustomConfigAIAgent> logger)
        {
        }

        public override Task<string> GetDescriptionAsync()
        {
            return Task.FromResult($"Custom AI Agent for {State.BusinessDomain} business domain with advanced configuration support.");
        }

        // 重写配置处理方法
        protected override async Task PerformConfigAsync(MyCustomConfig configuration)
        {
            Logger.LogInformation("Configuring AI Agent with custom settings for domain: {Domain}", configuration.BusinessDomain);

            // 初始化基础AI功能
            await InitializeAsync(new InitializeDto()
            {
                Instructions = configuration.Instructions,
                LLMConfig = configuration.LLMConfig,
                StreamingModeEnabled = configuration.StreamingModeEnabled,
                StreamingConfig = configuration.StreamingConfig
            });

            // 应用自定义配置
            RaiseEvent(new MyCustomAIAgentLogEvent 
            { 
                Operation = "Configure",
                Success = true,
                OperationData = new Dictionary<string, object>
                {
                    ["BusinessDomain"] = configuration.BusinessDomain,
                    ["MaxRetryAttempts"] = configuration.MaxRetryAttempts,
                    ["TimeoutDuration"] = configuration.TimeoutDuration.ToString(),
                    ["AllowedOperations"] = configuration.AllowedOperations,
                    ["EnableAdvancedFeatures"] = configuration.EnableAdvancedFeatures,
                    ["PriorityLevel"] = configuration.PriorityLevel,
                    ["CustomSettings"] = configuration.CustomSettings
                }
            });

            await ConfirmEvents();

            Logger.LogInformation("AI Agent configured successfully with {OperationCount} allowed operations", 
                configuration.AllowedOperations.Count);
        }

        // 基础聊天功能
        public async Task<string?> ChatAsync(string message)
        {
            try
            {
                var result = await ChatWithHistory(message);
                
                RaiseEvent(new MyCustomAIAgentLogEvent 
                { 
                    Operation = "Chat",
                    Success = result != null,
                    OperationData = new Dictionary<string, object> { ["Message"] = message, ["HasResult"] = result != null }
                });
                
                await ConfirmEvents();
                return result?[0].Content;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during chat");
                return $"Error: {ex.Message}";
            }
        }

        // 处理业务请求
        public async Task<string?> ProcessBusinessRequestAsync(string request, Dictionary<string, object>? parameters = null)
        {
            try
            {
                Logger.LogInformation("Processing business request in domain: {Domain}", State.BusinessDomain);

                var prompt = $@"
You are a specialized AI assistant for the {State.BusinessDomain} domain.
Priority Level: {State.PriorityLevel}
Advanced Features: {(State.IsAdvancedModeActive ? "Enabled" : "Disabled")}

Business Request: {request}

Additional Parameters: {(parameters != null ? string.Join(", ", parameters.Select(p => $"{p.Key}: {p.Value}")) : "None")}

Please provide a detailed response specific to the {State.BusinessDomain} domain.
                ";

                var result = await ChatWithHistory(prompt);
                
                RaiseEvent(new MyCustomAIAgentLogEvent 
                { 
                    Operation = "ProcessBusinessRequest",
                    Success = result != null,
                    OperationData = new Dictionary<string, object> 
                    { 
                        ["Request"] = request, 
                        ["HasResult"] = result != null,
                        ["Parameters"] = parameters ?? new Dictionary<string, object>()
                    }
                });
                
                await ConfirmEvents();

                return result?[0].Content;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing business request");
                
                RaiseEvent(new MyCustomAIAgentLogEvent 
                { 
                    Operation = "ProcessBusinessRequest",
                    Success = false,
                    ErrorMessage = ex.Message
                });
                
                await ConfirmEvents();
                return $"Error processing request: {ex.Message}";
            }
        }

        // 执行操作
        public async Task<bool> ExecuteOperationAsync(string operation, Dictionary<string, object> parameters)
        {
            try
            {
                Logger.LogInformation("Executing operation: {Operation}", operation);

                // 检查操作权限
                if (State.AllowedOperations.Count > 0 && !State.AllowedOperations.Contains(operation))
                {
                    Logger.LogWarning("Operation {Operation} not allowed", operation);
                    return false;
                }

                // 模拟操作执行
                await Task.Delay(100);

                RaiseEvent(new MyCustomAIAgentLogEvent 
                { 
                    Operation = operation,
                    Success = true,
                    OperationData = parameters
                });

                await ConfirmEvents();

                Logger.LogInformation("Operation {Operation} executed successfully", operation);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing operation: {Operation}", operation);
                
                RaiseEvent(new MyCustomAIAgentLogEvent 
                { 
                    Operation = operation,
                    Success = false,
                    ErrorMessage = ex.Message
                });
                
                await ConfirmEvents();
                return false;
            }
        }

        // 获取操作结果
        public async Task<Dictionary<string, object>> GetOperationResultsAsync()
        {
            await Task.CompletedTask;
            return State.OperationResults;
        }

        // 配置高级功能
        public async Task<bool> ConfigureAdvancedFeaturesAsync(bool enable)
        {
            RaiseEvent(new MyCustomAIAgentLogEvent 
            { 
                Operation = "ConfigureAdvancedFeatures",
                Success = true,
                OperationData = new Dictionary<string, object> { ["Enable"] = enable }
            });

            await ConfirmEvents();

            Logger.LogInformation("Advanced features {Status}", enable ? "enabled" : "disabled");
            return true;
        }

        // 事件处理器
        [EventHandler]
        public async Task OnCustomBusinessEvent(CustomBusinessEvent @event)
        {
            Logger.LogInformation("Received business event: {Operation} with priority {Priority}", 
                @event.Operation, @event.Priority);

            await ExecuteOperationAsync(@event.Operation, @event.Parameters);
        }

        // 状态转换
        protected override void AIGAgentTransitionState(MyCustomAIAgentState state, 
            StateLogEventBase<MyCustomAIAgentLogEvent> @event)
        {
            switch (@event)
            {
                case MyCustomAIAgentLogEvent logEvent:
                    state.LastOperationTime = logEvent.Timestamp;
                    state.ExecutedOperations.Add($"{logEvent.Operation}:{logEvent.Timestamp}");
                    
                    if (logEvent.Operation == "Configure" && logEvent.OperationData != null)
                    {
                        if (logEvent.OperationData.TryGetValue("BusinessDomain", out var domain))
                            state.BusinessDomain = domain.ToString();
                        if (logEvent.OperationData.TryGetValue("PriorityLevel", out var priority))
                            state.PriorityLevel = Convert.ToInt32(priority);
                        if (logEvent.OperationData.TryGetValue("EnableAdvancedFeatures", out var advanced))
                            state.IsAdvancedModeActive = Convert.ToBoolean(advanced);
                        if (logEvent.OperationData.TryGetValue("AllowedOperations", out var operations))
                            state.AllowedOperations = (List<string>)operations;
                        if (logEvent.OperationData.TryGetValue("CustomSettings", out var settings))
                            state.CustomSettings = (Dictionary<string, string>)settings;
                    }
                    
                    if (logEvent.Operation == "ConfigureAdvancedFeatures" && logEvent.OperationData != null)
                    {
                        if (logEvent.OperationData.TryGetValue("Enable", out var enable))
                            state.IsAdvancedModeActive = Convert.ToBoolean(enable);
                    }
                    
                    if (!logEvent.Success)
                    {
                        state.CurrentRetryCount++;
                    }
                    else
                    {
                        state.CurrentRetryCount = 0;
                    }
                    
                    // 存储操作结果
                    state.OperationResults[logEvent.Operation] = new 
                    { 
                        Success = logEvent.Success, 
                        Timestamp = logEvent.Timestamp,
                        Data = logEvent.OperationData,
                        Error = logEvent.ErrorMessage
                    };
                    break;
            }
        }
    }

    // 7. 单元测试类
    public class CustomConfigAIAgentTest : AevatarGAgentTestBase<CustomConfigAIAgentTestModule>
    {
        private readonly IGAgentFactory _agentFactory;
        private readonly ITestOutputHelper _output;

        public CustomConfigAIAgentTest(ITestOutputHelper output)
        {
            _output = output;
            _agentFactory = GetRequiredService<IGAgentFactory>();
        }

        [Fact]
        public async Task Should_Configure_CustomConfig_Successfully()
        {
            // Arrange
            _output.WriteLine("Testing custom configuration...");
            var agentId = Guid.NewGuid();
            var agent = await _agentFactory.GetGAgentAsync<IMyCustomConfigAIAgent>(agentId);

            var customConfig = new MyCustomConfig
            {
                Instructions = "You are a specialized AI assistant for E-Commerce operations.",
                LLMConfig = new LLMConfigDto { SystemLLM = "Mock" }, // 使用Mock避免真实API调用
                BusinessDomain = "E-Commerce",
                MaxRetryAttempts = 5,
                TimeoutDuration = TimeSpan.FromMinutes(10),
                AllowedOperations = new List<string> { "ProcessOrder", "HandleQuery", "GenerateReport" },
                CustomSettings = new Dictionary<string, string> 
                { 
                    ["CompanyName"] = "TestStore",
                    ["Region"] = "Test Region"
                },
                EnableAdvancedFeatures = true,
                PriorityLevel = 3
            };

            // Act
            var result = await agent.ConfigureAsync(customConfig);
            var state = await agent.GetStateAsync();

            // Assert
            result.ShouldBe(true);
            state.BusinessDomain.ShouldBe("E-Commerce");
            state.PriorityLevel.ShouldBe(3);
            state.IsAdvancedModeActive.ShouldBe(true);
            state.AllowedOperations.Count.ShouldBe(3);
            state.AllowedOperations.ShouldContain("ProcessOrder");
            state.CustomSettings["CompanyName"].ShouldBe("TestStore");
            
            _output.WriteLine($"✅ Configuration successful! Domain: {state.BusinessDomain}, Priority: {state.PriorityLevel}");
        }

        [Fact]
        public async Task Should_Process_Business_Request_Successfully()
        {
            // Arrange
            _output.WriteLine("Testing business request processing...");
            var agentId = Guid.NewGuid();
            var agent = await _agentFactory.GetGAgentAsync<IMyCustomConfigAIAgent>(agentId);

            await ConfigureTestAgent(agent);

            var request = "How should I handle a customer refund request?";
            var parameters = new Dictionary<string, object> 
            { 
                ["OrderId"] = "TEST-001",
                ["Amount"] = 99.99
            };

            // Act
            var response = await agent.ProcessBusinessRequestAsync(request, parameters);
            var state = await agent.GetStateAsync();

            // Assert
            response.ShouldNotBeNull();
            response.ShouldNotBeEmpty();
            state.ExecutedOperations.Count.ShouldBeGreaterThan(0);
            state.OperationResults.ShouldContainKey("ProcessBusinessRequest");
            
            _output.WriteLine($"✅ Business request processed! Response length: {response.Length}");
        }

        [Fact]
        public async Task Should_Execute_Allowed_Operations_Successfully()
        {
            // Arrange
            _output.WriteLine("Testing operation execution...");
            var agentId = Guid.NewGuid();
            var agent = await _agentFactory.GetGAgentAsync<IMyCustomConfigAIAgent>(agentId);

            await ConfigureTestAgent(agent);

            var operation = "ProcessOrder";
            var parameters = new Dictionary<string, object> 
            { 
                ["OrderId"] = "TEST-002",
                ["CustomerEmail"] = "test@example.com"
            };

            // Act
            var result = await agent.ExecuteOperationAsync(operation, parameters);
            var state = await agent.GetStateAsync();

            // Assert
            result.ShouldBe(true);
            state.ExecutedOperations.ShouldContain(op => op.Contains("ProcessOrder"));
            state.OperationResults.ShouldContainKey("ProcessOrder");
            
            _output.WriteLine($"✅ Operation executed successfully! Total operations: {state.ExecutedOperations.Count}");
        }

        [Fact]
        public async Task Should_Reject_Disallowed_Operations()
        {
            // Arrange
            _output.WriteLine("Testing operation permission control...");
            var agentId = Guid.NewGuid();
            var agent = await _agentFactory.GetGAgentAsync<IMyCustomConfigAIAgent>(agentId);

            await ConfigureTestAgent(agent);

            var disallowedOperation = "DeleteUser"; // 不在AllowedOperations中
            var parameters = new Dictionary<string, object> { ["UserId"] = "test123" };

            // Act
            var result = await agent.ExecuteOperationAsync(disallowedOperation, parameters);

            // Assert
            result.ShouldBe(false);
            
            _output.WriteLine($"✅ Disallowed operation correctly rejected!");
        }

        [Fact]
        public async Task Should_Configure_Advanced_Features()
        {
            // Arrange
            _output.WriteLine("Testing advanced features configuration...");
            var agentId = Guid.NewGuid();
            var agent = await _agentFactory.GetGAgentAsync<IMyCustomConfigAIAgent>(agentId);

            await ConfigureTestAgent(agent);

            // Act - 禁用高级功能
            var disableResult = await agent.ConfigureAdvancedFeaturesAsync(false);
            var stateAfterDisable = await agent.GetStateAsync();

            // 启用高级功能
            var enableResult = await agent.ConfigureAdvancedFeaturesAsync(true);
            var stateAfterEnable = await agent.GetStateAsync();

            // Assert
            disableResult.ShouldBe(true);
            stateAfterDisable.IsAdvancedModeActive.ShouldBe(false);
            
            enableResult.ShouldBe(true);
            stateAfterEnable.IsAdvancedModeActive.ShouldBe(true);
            
            _output.WriteLine("✅ Advanced features configuration working correctly!");
        }

        [Fact]
        public async Task Should_Handle_Business_Events()
        {
            // Arrange
            _output.WriteLine("Testing business event handling...");
            var agentId = Guid.NewGuid();
            var agent = await _agentFactory.GetGAgentAsync<IMyCustomConfigAIAgent>(agentId);

            await ConfigureTestAgent(agent);

            var businessEvent = new CustomBusinessEvent
            {
                Operation = "ProcessOrder",
                Parameters = new Dictionary<string, object> 
                { 
                    ["OrderId"] = "EVENT-001",
                    ["Amount"] = 156.78
                },
                Priority = 2
            };

            // Act
            await agent.PublishAsync(businessEvent);
            
            // 等待事件处理
            await Task.Delay(1000);
            
            var state = await agent.GetStateAsync();

            // Assert
            state.ExecutedOperations.ShouldContain(op => op.Contains("ProcessOrder"));
            
            _output.WriteLine("✅ Business event handled successfully!");
        }

        [Fact]
        public async Task Should_Get_Operation_Results()
        {
            // Arrange
            _output.WriteLine("Testing operation results retrieval...");
            var agentId = Guid.NewGuid();
            var agent = await _agentFactory.GetGAgentAsync<IMyCustomConfigAIAgent>(agentId);

            await ConfigureTestAgent(agent);

            // 执行一些操作
            await agent.ExecuteOperationAsync("ProcessOrder", new Dictionary<string, object> { ["OrderId"] = "RESULT-001" });
            await agent.ExecuteOperationAsync("GenerateReport", new Dictionary<string, object> { ["ReportType"] = "Sales" });

            // Act
            var results = await agent.GetOperationResultsAsync();

            // Assert
            results.ShouldNotBeEmpty();
            results.ShouldContainKey("ProcessOrder");
            results.ShouldContainKey("GenerateReport");
            results.ShouldContainKey("Configure"); // 配置操作也会被记录
            
            _output.WriteLine($"✅ Operation results retrieved! Total results: {results.Count}");
        }

        // 辅助方法：配置测试Agent
        private async Task ConfigureTestAgent(IMyCustomConfigAIAgent agent)
        {
            var config = new MyCustomConfig
            {
                Instructions = "You are a test AI assistant for E-Commerce operations.",
                LLMConfig = new LLMConfigDto { SystemLLM = "Mock" },
                BusinessDomain = "E-Commerce",
                MaxRetryAttempts = 3,
                AllowedOperations = new List<string> { "ProcessOrder", "HandleQuery", "GenerateReport" },
                CustomSettings = new Dictionary<string, string> { ["TestMode"] = "true" },
                EnableAdvancedFeatures = true,
                PriorityLevel = 2
            };

            await agent.ConfigureAsync(config);
        }
    }

    // 8. 测试模块配置
    public class CustomConfigAIAgentTestModule : AevatarGAgentTestBaseModule
    {
        // 可以在这里添加额外的测试配置
    }
} 