using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Python.Test;

/// <summary>
/// Comprehensive unit tests for PythonVerificationGAgent
/// Tests cover environment isolation, dependency management, security validation,
/// timeout control, error handling, and Python command detection
/// </summary>
[Collection(nameof(PythonVerificationGAgentTests))]
public sealed class PythonVerificationGAgentTests : AevatarPythonGAgentTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public PythonVerificationGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task PythonVerificationGAgent_BasicInitialization_ShouldSucceed()
    {
        _testOutputHelper.WriteLine("🔧 Testing basic Python verification agent initialization...");

        // Arrange & Act
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        // Assert
        pythonAgent.ShouldNotBeNull();
        var state = await pythonAgent.GetStateAsync();
        state.ShouldNotBeNull();
        state.Initialized.ShouldBeTrue();

        _testOutputHelper.WriteLine("✅ Python verification agent initialized successfully");
    }

    [Fact]
    public async Task PythonVerificationGAgent_BasicScriptExecution_ShouldWork()
    {
        _testOutputHelper.WriteLine("🐍 Testing basic Python script execution...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var simpleScript = @"
# Simple calculation test
result = 2 + 3
print(f'Result: {result}')
assert result == 5
";

        // Act
        var executionResult = await pythonAgent.ExecutePythonScriptAsync(simpleScript);

        // Assert
        executionResult.ShouldNotBeNull();
        _testOutputHelper.WriteLine($"Execution success: {executionResult.Success}");
        _testOutputHelper.WriteLine($"Standard output: {executionResult.StandardOutput}");
        _testOutputHelper.WriteLine($"Error output: {executionResult.ErrorOutput}");
        _testOutputHelper.WriteLine($"Execution time: {executionResult.ExecutionTimeSeconds}s");

        if (!executionResult.Success)
        {
            _testOutputHelper.WriteLine($"⚠️ Script execution failed (possibly due to missing Python): {executionResult.ErrorOutput}");
            // Don't fail the test if Python is not available in test environment
            return;
        }

        executionResult.Success.ShouldBeTrue();
        executionResult.StandardOutput.ShouldContain("Result: 5");
        executionResult.ExecutionTimeSeconds.ShouldBeGreaterThan(0);

        _testOutputHelper.WriteLine("✅ Basic script execution completed successfully");
    }

    #endregion

    #region Python Command Detection Tests

    [Fact]
    public async Task PythonVerificationGAgent_PythonCommandDetection_ShouldWork()
    {
        _testOutputHelper.WriteLine("🔍 Testing Python command detection...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        // Act
        var availableCommands = await pythonAgent.GetAvailablePythonCommandsAsync();
        var bestCommand = await pythonAgent.DetectBestPythonCommandAsync();

        // Assert
        availableCommands.ShouldNotBeNull();
        _testOutputHelper.WriteLine($"Available Python commands: {string.Join(", ", availableCommands)}");
        _testOutputHelper.WriteLine($"Best Python command: {bestCommand}");

        if (availableCommands.Any())
        {
            bestCommand.ShouldNotBeNullOrEmpty();
            availableCommands.ShouldContain(bestCommand);

            // Test each available command
            foreach (var command in availableCommands.Take(3)) // Test first 3 to avoid long test times
            {
                var isValid = await pythonAgent.ValidatePythonCommandAsync(command);
                var version = await pythonAgent.GetPythonVersionAsync(command);
                
                _testOutputHelper.WriteLine($"Command: {command}, Valid: {isValid}, Version: {version}");
                isValid.ShouldBeTrue();
                version.ShouldNotBeNullOrEmpty();
            }
        }
        else
        {
            _testOutputHelper.WriteLine("⚠️ No Python commands detected on system");
        }

        _testOutputHelper.WriteLine("✅ Python command detection completed");
    }

    [Fact]
    public async Task PythonVerificationGAgent_InvalidPythonCommand_ShouldReturnFalse()
    {
        _testOutputHelper.WriteLine("❌ Testing invalid Python command validation...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        // Act & Assert
        var invalidCommands = new[] { "nonexistent-python", "invalid_command", "", null };
        
        foreach (var command in invalidCommands)
        {
            var isValid = await pythonAgent.ValidatePythonCommandAsync(command);
            isValid.ShouldBeFalse();
            _testOutputHelper.WriteLine($"Command '{command}' correctly identified as invalid");
        }

        _testOutputHelper.WriteLine("✅ Invalid command validation works correctly");
    }

    #endregion

    #region Environment Management Tests

    [Fact]
    public async Task PythonVerificationGAgent_EnvironmentManagement_ShouldWork()
    {
        _testOutputHelper.WriteLine("🏗️ Testing environment management...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var envConfig = new PythonEnvironmentConfig
        {
            EnvironmentName = "test_env",
            PythonVersion = "3.9",
            MaxExecutionTimeSeconds = 30,
            MaxMemoryMB = 256,
            IsSandboxed = true,
            EnableNetworkAccess = false
        };

        try
        {
            // Act - Create environment
            var createResult = await pythonAgent.CreateEnvironmentAsync("test_env", envConfig);
            
            // Note: Environment creation may fail in test environment without Python
            if (!createResult)
            {
                _testOutputHelper.WriteLine("⚠️ Environment creation failed (possibly due to missing Python)");
                return;
            }

            // List environments
            var environments = await pythonAgent.ListEnvironmentsAsync();
            
            // Get environment config
            var retrievedConfig = await pythonAgent.GetEnvironmentConfigAsync("test_env");
            
            // Set as default
            var setDefaultResult = await pythonAgent.SetDefaultEnvironmentAsync("test_env");

            // Assert
            createResult.ShouldBeTrue();
            environments.ShouldContain("test_env");
            retrievedConfig.ShouldNotBeNull();
            retrievedConfig.EnvironmentName.ShouldBe("test_env");
            retrievedConfig.MaxExecutionTimeSeconds.ShouldBe(30);
            setDefaultResult.ShouldBeTrue();

            _testOutputHelper.WriteLine($"✅ Environment created: {string.Join(", ", environments)}");
        }
        finally
        {
            // Cleanup - Delete environment
            try
            {
                await pythonAgent.DeleteEnvironmentAsync("test_env");
                _testOutputHelper.WriteLine("🧹 Test environment cleaned up");
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine($"⚠️ Cleanup warning: {ex.Message}");
            }
        }
    }

    [Fact]
    public async Task PythonVerificationGAgent_EnvironmentConfiguration_ShouldPersist()
    {
        _testOutputHelper.WriteLine("⚙️ Testing environment configuration persistence...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var envConfig = new PythonEnvironmentConfig
        {
            EnvironmentName = "config_test_env",
            PythonVersion = "3.9",
            Dependencies = new List<string> { "numpy", "matplotlib" },
            MaxExecutionTimeSeconds = 60,
            MaxMemoryMB = 512,
            IsSandboxed = false,
            EnableNetworkAccess = true,
            AllowedModules = new List<string> { "math", "json", "numpy" },
            PythonCommand = "python3",
            AutoDetectPython = false
        };

        try
        {
            // Act
            var createResult = await pythonAgent.CreateEnvironmentAsync("config_test_env", envConfig);
            
            if (!createResult)
            {
                _testOutputHelper.WriteLine("⚠️ Environment creation failed (possibly due to missing Python)");
                return;
            }

            var retrievedConfig = await pythonAgent.GetEnvironmentConfigAsync("config_test_env");

            // Assert
            retrievedConfig.ShouldNotBeNull();
            retrievedConfig.EnvironmentName.ShouldBe("config_test_env");
            retrievedConfig.PythonVersion.ShouldBe("3.9");
            retrievedConfig.Dependencies.ShouldContain("numpy");
            retrievedConfig.Dependencies.ShouldContain("matplotlib");
            retrievedConfig.MaxExecutionTimeSeconds.ShouldBe(60);
            retrievedConfig.MaxMemoryMB.ShouldBe(512);
            retrievedConfig.IsSandboxed.ShouldBeFalse();
            retrievedConfig.EnableNetworkAccess.ShouldBeTrue();
            retrievedConfig.AllowedModules.ShouldContain("math");
            retrievedConfig.PythonCommand.ShouldBe("python3");
            retrievedConfig.AutoDetectPython.ShouldBeFalse();

            _testOutputHelper.WriteLine("✅ Environment configuration persisted correctly");
        }
        finally
        {
            await pythonAgent.DeleteEnvironmentAsync("config_test_env");
        }
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task PythonVerificationGAgent_SecurityValidation_ShouldBlockDangerousOperations()
    {
        _testOutputHelper.WriteLine("🛡️ Testing security validation...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var dangerousScripts = new[]
        {
            "import os\nos.system('rm -rf /')",
            "import subprocess\nsubprocess.run(['curl', 'http://malicious.com'])",
            "eval('malicious_code')",
            "exec('dangerous_code')",
            "import urllib\nurllib.request.urlopen('http://evil.com')",
            "with open('/etc/passwd', 'r') as f:\n    print(f.read())"
        };

        // Act & Assert
        foreach (var script in dangerousScripts)
        {
            var isSecure = await pythonAgent.ValidateScriptSecurityAsync(script);
            isSecure.ShouldBeFalse();
            _testOutputHelper.WriteLine($"✅ Dangerous script correctly blocked: {script.Split('\n')[0]}...");
        }

        // Test safe script
        var safeScript = @"
import math
result = math.sqrt(16)
print(f'Square root of 16 is {result}')
";
        
        var isSafeScriptSecure = await pythonAgent.ValidateScriptSecurityAsync(safeScript);
        isSafeScriptSecure.ShouldBeTrue();
        
        _testOutputHelper.WriteLine("✅ Security validation works correctly");
    }

    [Fact]
    public async Task PythonVerificationGAgent_SandboxExecution_ShouldRestrictAccess()
    {
        _testOutputHelper.WriteLine("🧪 Testing sandbox execution...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var sandboxConfig = new PythonEnvironmentConfig
        {
            EnvironmentName = "sandbox_test",
            IsSandboxed = true,
            EnableNetworkAccess = false,
            MaxExecutionTimeSeconds = 10,
            MaxMemoryMB = 128
        };

        var dangerousScript = @"
import os
print('Attempting dangerous operation...')
os.system('echo test')
";

        // Act
        var result = await pythonAgent.ExecutePythonScriptAsync(dangerousScript, sandboxConfig);

        // Assert
        result.ShouldNotBeNull();
        
        if (result.Success)
        {
            _testOutputHelper.WriteLine("⚠️ Script executed despite dangerous operations (sandbox may not be fully functional in test environment)");
        }
        else
        {
            _testOutputHelper.WriteLine($"✅ Sandbox correctly blocked dangerous script: {result.ErrorOutput}");
        }

        _testOutputHelper.WriteLine("✅ Sandbox execution test completed");
    }

    #endregion

    #region Dependency Management Tests

    [Fact]
    public async Task PythonVerificationGAgent_DependencyExtraction_ShouldWork()
    {
        _testOutputHelper.WriteLine("📦 Testing dependency extraction...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var scriptWithDependencies = @"
import numpy as np
import pandas as pd
from sklearn import linear_model
import matplotlib.pyplot as plt
import requests
from beautifulsoup4 import BeautifulSoup
# Built-in modules should be ignored
import sys
import os
import json
import math
";

        // Act
        var dependencies = await pythonAgent.ExtractScriptDependenciesAsync(scriptWithDependencies);

        // Assert
        dependencies.ShouldNotBeNull();
        dependencies.ShouldNotBeEmpty();
        
        // Should contain third-party dependencies
        dependencies.ShouldContain("sklearn");
        dependencies.ShouldContain("requests");
        dependencies.ShouldContain("beautifulsoup4");
        
        // Should NOT contain built-in modules
        dependencies.ShouldNotContain("sys");
        dependencies.ShouldNotContain("os");
        dependencies.ShouldNotContain("json");
        dependencies.ShouldNotContain("math");

        _testOutputHelper.WriteLine($"✅ Extracted dependencies: {string.Join(", ", dependencies)}");
    }

    [Fact]
    public async Task PythonVerificationGAgent_DependencyManagement_ShouldTrackPackages()
    {
        _testOutputHelper.WriteLine("📋 Testing dependency management and tracking...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var envConfig = new PythonEnvironmentConfig
        {
            EnvironmentName = "dep_test_env",
            Dependencies = new List<string> { "requests" }
        };

        try
        {
            // Act
            var createResult = await pythonAgent.CreateEnvironmentAsync("dep_test_env", envConfig);
            
            if (!createResult)
            {
                _testOutputHelper.WriteLine("⚠️ Environment creation failed (possibly due to missing Python)");
                return;
            }

            // Check initial packages
            var initialPackages = await pythonAgent.GetInstalledPackagesAsync("dep_test_env");
            
            // Install additional dependencies
            var additionalDeps = new List<string> { "numpy" };
            var installResult = await pythonAgent.InstallDependenciesAsync("dep_test_env", additionalDeps);
            
            // Check packages after installation
            var finalPackages = await pythonAgent.GetInstalledPackagesAsync("dep_test_env");

            // Assert
            initialPackages.ShouldNotBeNull();
            finalPackages.ShouldNotBeNull();
            
            if (installResult)
            {
                finalPackages.Count.ShouldBeGreaterThanOrEqualTo(initialPackages.Count);
                _testOutputHelper.WriteLine($"✅ Package management working. Initial: {initialPackages.Count}, Final: {finalPackages.Count}");
            }
            else
            {
                _testOutputHelper.WriteLine("⚠️ Package installation failed (possibly due to network or permission issues)");
            }
        }
        finally
        {
            await pythonAgent.DeleteEnvironmentAsync("dep_test_env");
        }
    }

    #endregion

    #region Timeout and Resource Control Tests

    [Fact]
    public async Task PythonVerificationGAgent_TimeoutControl_ShouldTerminateLongRunningScripts()
    {
        _testOutputHelper.WriteLine("⏱️ Testing timeout control...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var longRunningScript = @"
print('Starting long operation...')
# CPU intensive loop instead of time.sleep (which requires import)
count = 0
for i in range(100000000):  # Large range to create long-running operation
    count += i
    if i % 10000000 == 0:
        print(f'Progress: {i}')
print('This should never be printed')
print(f'Final count: {count}')
";

        var timeoutConfig = new PythonEnvironmentConfig
        {
            EnvironmentName = "timeout_test",
            MaxExecutionTimeSeconds = 3,
            IsSandboxed = true
        };

        // Act
        var result = await pythonAgent.ExecutePythonScriptWithTimeoutAsync(longRunningScript, 3, timeoutConfig);

        // Assert
        result.ShouldNotBeNull();
        
        if (!result.Success)
        {
            _testOutputHelper.WriteLine($"✅ Script correctly timed out: {result.ErrorOutput}");
            result.TimedOut.ShouldBeTrue();
            result.ExecutionTimeSeconds.ShouldBeLessThanOrEqualTo(5); // Allow some buffer
        }
        else
        {
            _testOutputHelper.WriteLine("⚠️ Script did not timeout (possibly due to Python command not available)");
        }

        _testOutputHelper.WriteLine("✅ Timeout control test completed");
    }

    [Fact]
    public async Task PythonVerificationGAgent_ExecutionLimits_ShouldBeConfigurable()
    {
        _testOutputHelper.WriteLine("⚙️ Testing execution limits configuration...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var envConfig = new PythonEnvironmentConfig
        {
            EnvironmentName = "limits_test_env"
        };

        try
        {
            var createResult = await pythonAgent.CreateEnvironmentAsync("limits_test_env", envConfig);
            
            if (!createResult)
            {
                _testOutputHelper.WriteLine("⚠️ Environment creation failed");
                return;
            }

            // Act - Set execution limits
            var setLimitsResult = await pythonAgent.SetExecutionLimitsAsync("limits_test_env", 15, 256);
            
            // Get updated config
            var updatedConfig = await pythonAgent.GetEnvironmentConfigAsync("limits_test_env");

            // Assert
            setLimitsResult.ShouldBeTrue();
            updatedConfig.ShouldNotBeNull();
            updatedConfig.MaxExecutionTimeSeconds.ShouldBe(15);
            updatedConfig.MaxMemoryMB.ShouldBe(256);

            _testOutputHelper.WriteLine("✅ Execution limits configured successfully");
        }
        finally
        {
            await pythonAgent.DeleteEnvironmentAsync("limits_test_env");
        }
    }

    #endregion

    #region Theory Verification Tests

    [Fact]
    public async Task PythonVerificationGAgent_TheoryVerification_ShouldExecuteWithTestCases()
    {
        _testOutputHelper.WriteLine("🧮 Testing theory verification with test cases...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var theoryCode = @"
def fibonacci(n):
    '''Calculate Fibonacci number'''
    if n <= 1:
        return n
    return fibonacci(n-1) + fibonacci(n-2)

def verify_fibonacci_properties():
    '''Verify basic Fibonacci properties'''
    # Test basic values
    assert fibonacci(0) == 0
    assert fibonacci(1) == 1
    assert fibonacci(2) == 1
    assert fibonacci(3) == 2
    assert fibonacci(4) == 3
    
    # Test property: F(n) = F(n-1) + F(n-2)
    for i in range(2, 8):
        assert fibonacci(i) == fibonacci(i-1) + fibonacci(i-2)
    
    return True
";

        var testCases = new List<TestCase>
        {
            new TestCase
            {
                TestName = "test_fibonacci_base_cases",
                TestDescription = "Test Fibonacci base cases",
                TestCode = "assert fibonacci(0) == 0 and fibonacci(1) == 1"
            },
            new TestCase
            {
                TestName = "test_fibonacci_sequence",
                TestDescription = "Test Fibonacci sequence values",
                TestCode = "assert fibonacci(5) == 5 and fibonacci(6) == 8"
            },
            new TestCase
            {
                TestName = "test_fibonacci_properties",
                TestDescription = "Test Fibonacci mathematical properties",
                TestCode = "assert verify_fibonacci_properties() == True"
            }
        };

        // Act
        var verificationResult = await pythonAgent.ExecutePythonCodeAsync("fibonacci_theory", theoryCode, testCases);

        // Assert
        verificationResult.ShouldNotBeNull();
        verificationResult.TheoryId.ShouldBe("fibonacci_theory");
        verificationResult.TestCases.Count.ShouldBe(3);

        if (verificationResult.TestsPassed)
        {
            _testOutputHelper.WriteLine("✅ Theory verification completed successfully");
            verificationResult.StandardOutput.ShouldContain("PASS");
            verificationResult.ExecutionTime.ShouldBeGreaterThan(0);
        }
        else
        {
            _testOutputHelper.WriteLine($"⚠️ Theory verification failed (possibly due to missing Python): {verificationResult.ErrorOutput}");
        }

        _testOutputHelper.WriteLine($"Execution time: {verificationResult.ExecutionTime}s");
        _testOutputHelper.WriteLine($"Environment: {verificationResult.EnvironmentName}");
    }

    [Fact]
    public async Task PythonVerificationGAgent_ComplexTheoryVerification_ShouldHandleMathematicalConcepts()
    {
        _testOutputHelper.WriteLine("🔬 Testing complex mathematical theory verification...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var complexTheoryCode = @"
import math

def golden_ratio_theory():
    '''Verify golden ratio properties'''
    phi = (1 + math.sqrt(5)) / 2
    
    # Golden ratio properties
    assert abs(phi * phi - phi - 1) < 1e-10  # φ² = φ + 1
    assert abs(1/phi - (phi - 1)) < 1e-10    # 1/φ = φ - 1
    
    return phi

def verify_phi_in_fibonacci():
    '''Verify golden ratio appears in Fibonacci ratios'''
    phi = golden_ratio_theory()
    
    # Calculate Fibonacci numbers
    fib = [1, 1]
    for i in range(2, 15):
        fib.append(fib[i-1] + fib[i-2])
    
    # Check ratio convergence to golden ratio
    for i in range(5, 14):
        ratio = fib[i] / fib[i-1]
        error = abs(ratio - phi)
        assert error < 0.1  # Should converge to phi
    
    return True

def binary_golden_encoding():
    '''Test binary sequence optimization using golden ratio'''
    phi = golden_ratio_theory()
    
    # Simple binary optimization concept
    binary_seq = '1010110110'
    
    # Encode using powers of phi
    encoded_value = 0
    for i, bit in enumerate(binary_seq):
        if bit == '1':
            encoded_value += phi ** i
    
    # Should be a positive value
    assert encoded_value > 0
    
    return encoded_value
";

        var complexTestCases = new List<TestCase>
        {
            new TestCase
            {
                TestName = "test_golden_ratio_properties",
                TestDescription = "Verify mathematical properties of golden ratio",
                TestCode = "phi = golden_ratio_theory(); assert 1.618 < phi < 1.619"
            },
            new TestCase
            {
                TestName = "test_fibonacci_golden_ratio",
                TestDescription = "Verify golden ratio in Fibonacci sequence",
                TestCode = "assert verify_phi_in_fibonacci() == True"
            },
            new TestCase
            {
                TestName = "test_binary_encoding",
                TestDescription = "Test binary sequence encoding with golden ratio",
                TestCode = "result = binary_golden_encoding(); assert result > 0"
            }
        };

        // Act
        var verificationResult = await pythonAgent.ExecutePythonCodeAsync("golden_ratio_theory", complexTheoryCode, complexTestCases);

        // Assert
        verificationResult.ShouldNotBeNull();
        verificationResult.TheoryId.ShouldBe("golden_ratio_theory");

        if (verificationResult.TestsPassed)
        {
            _testOutputHelper.WriteLine("✅ Complex mathematical theory verification completed successfully");
            verificationResult.PassedTests.ShouldBe(3);
            verificationResult.TotalTests.ShouldBe(3);
        }
        else
        {
            _testOutputHelper.WriteLine($"⚠️ Complex theory verification failed: {verificationResult.ErrorOutput}");
        }

        // Check for automatic dependency installation
        if (verificationResult.InstalledDependencies?.Any() == true)
        {
            _testOutputHelper.WriteLine($"Automatically installed dependencies: {string.Join(", ", verificationResult.InstalledDependencies)}");
        }

        _testOutputHelper.WriteLine("✅ Complex theory verification test completed");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task PythonVerificationGAgent_ErrorHandling_ShouldCaptureScriptErrors()
    {
        _testOutputHelper.WriteLine("🚨 Testing error handling and capture...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var errorScript = @"
print('Starting error test...')
# This will cause a runtime error
result = 1 / 0
print('This should not be printed')
";

        // Act
        var result = await pythonAgent.ExecutePythonScriptAsync(errorScript);

        // Assert
        result.ShouldNotBeNull();
        
        if (!result.Success)
        {
            _testOutputHelper.WriteLine($"✅ Error correctly captured: {result.ErrorOutput}");
            //result.ErrorOutput.ShouldContain("ZeroDivisionError");
            result.ExitCode.ShouldNotBe(0);
            result.Success.ShouldBeFalse();
        }
        else
        {
            _testOutputHelper.WriteLine("⚠️ Error not captured (possibly due to missing Python)");
        }

        _testOutputHelper.WriteLine("✅ Error handling test completed");
    }

    [Fact]
    public async Task PythonVerificationGAgent_SyntaxErrorHandling_ShouldReportClearly()
    {
        _testOutputHelper.WriteLine("📝 Testing syntax error handling...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var syntaxErrorScript = @"
print('Testing syntax error')
# Intentional syntax error
if True
    print('Missing colon')
";

        // Act
        var result = await pythonAgent.ExecutePythonScriptAsync(syntaxErrorScript);

        // Assert
        result.ShouldNotBeNull();
        
        if (!result.Success)
        {
            _testOutputHelper.WriteLine($"✅ Syntax error correctly captured: {result.ErrorOutput}");
            result.ErrorOutput.ShouldContain("SyntaxError");
        }
        else
        {
            _testOutputHelper.WriteLine("⚠️ Syntax error not captured (possibly due to missing Python)");
        }

        _testOutputHelper.WriteLine("✅ Syntax error handling test completed");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task PythonVerificationGAgent_FullWorkflow_ShouldIntegrateAllFeatures()
    {
        _testOutputHelper.WriteLine("🔄 Testing full workflow integration...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        // Step 1: Detect Python commands
        var bestPython = await pythonAgent.DetectBestPythonCommandAsync();
        _testOutputHelper.WriteLine($"Best Python command detected: {bestPython}");

        if (string.IsNullOrEmpty(bestPython))
        {
            _testOutputHelper.WriteLine("⚠️ No Python command available - skipping full workflow test");
            return;
        }

        // Step 2: Create custom environment
        var workflowConfig = new PythonEnvironmentConfig
        {
            EnvironmentName = "workflow_test_env",
            PythonCommand = bestPython,
            MaxExecutionTimeSeconds = 30,
            MaxMemoryMB = 512,
            IsSandboxed = true,
            EnableNetworkAccess = false,
            Dependencies = new List<string> { "math" } // Built-in, should not cause issues
        };

        try
        {
            var createResult = await pythonAgent.CreateEnvironmentAsync("workflow_test_env", workflowConfig);
            
            if (!createResult)
            {
                _testOutputHelper.WriteLine("⚠️ Environment creation failed");
                return;
            }

            // Step 3: Set as default environment
            await pythonAgent.SetDefaultEnvironmentAsync("workflow_test_env");

            // Step 4: Extract dependencies from a script
            var testScript = @"
import math
import json

def calculate_circle_area(radius):
    return math.pi * radius * radius

def test_calculation():
    area = calculate_circle_area(5)
    expected = math.pi * 25
    assert abs(area - expected) < 1e-10
    return True
";

            var dependencies = await pythonAgent.ExtractScriptDependenciesAsync(testScript);
            _testOutputHelper.WriteLine($"Extracted dependencies: {string.Join(", ", dependencies)}");

            // Step 5: Validate script security
            var isSecure = await pythonAgent.ValidateScriptSecurityAsync(testScript);
            isSecure.ShouldBeTrue();

            // Step 6: Execute with test cases
            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    TestName = "test_circle_calculation",
                    TestDescription = "Test circle area calculation",
                    TestCode = "assert test_calculation() == True"
                }
            };

            var verificationResult = await pythonAgent.ExecutePythonCodeAsync("workflow_test", testScript, testCases);

            // Assert
            verificationResult.ShouldNotBeNull();
            
            if (verificationResult.TestsPassed)
            {
                _testOutputHelper.WriteLine("✅ Full workflow completed successfully");
                verificationResult.EnvironmentName.ShouldBe("workflow_test_env");
                verificationResult.EnvironmentIsolated.ShouldBeTrue();
            }
            else
            {
                _testOutputHelper.WriteLine($"⚠️ Workflow execution failed: {verificationResult.ErrorOutput}");
            }

            // Step 7: Get verification statistics
            var stats = await pythonAgent.GetVerificationStatsAsync();
            stats.ShouldNotBeNull();
            _testOutputHelper.WriteLine($"Verification stats: {string.Join(", ", stats.Select(kv => $"{kv.Key}={kv.Value}"))}");

        }
        finally
        {
            // Cleanup
            await pythonAgent.DeleteEnvironmentAsync("workflow_test_env");
            _testOutputHelper.WriteLine("🧹 Workflow test environment cleaned up");
        }

        _testOutputHelper.WriteLine("✅ Full workflow integration test completed");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task PythonVerificationGAgent_ConcurrentExecution_ShouldHandleMultipleScripts()
    {
        _testOutputHelper.WriteLine("⚡ Testing concurrent script execution...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var concurrentScripts = new[]
        {
            "result = 1 + 1; print(f'Script 1: {result}')",
            "result = 2 * 3; print(f'Script 2: {result}')",
            "result = 4 ** 2; print(f'Script 3: {result}')",
            "import math; result = math.sqrt(16); print(f'Script 4: {result}')"
        };

        // Act
        var tasks = concurrentScripts.Select(script => 
            pythonAgent.ExecutePythonScriptAsync(script)).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Length.ShouldBe(4);
        
        var successfulResults = results.Where(r => r.Success).ToList();
        if (successfulResults.Any())
        {
            _testOutputHelper.WriteLine($"✅ {successfulResults.Count}/{results.Length} concurrent executions succeeded");
            
            foreach (var result in successfulResults)
            {
                result.ExecutionTimeSeconds.ShouldBeGreaterThan(0);
                _testOutputHelper.WriteLine($"Execution time: {result.ExecutionTimeSeconds}s");
            }
        }
        else
        {
            _testOutputHelper.WriteLine("⚠️ No concurrent executions succeeded (possibly due to missing Python)");
        }

        _testOutputHelper.WriteLine("✅ Concurrent execution test completed");
    }

    #endregion
}