# Aevatar.GAgents.AI.Abstractions

Agent Information Management System for automatic Agent discovery and metadata extraction.

## 🚀 Quick Start

### 1. Mark Your Agent

```csharp
[AgentDescription(
    "Social Chat Agent",
    "AI-powered social platform chat agent with multi-turn conversation and emotion understanding capabilities.",
    "Detailed description of agent capabilities, features, and usage scenarios for LLM understanding..."
)]
public class SocialGAgent : AIGAgentBase<SocialGAgentState, SocialGAgentSEvent>
{
    // Agent implementation
}
```

### 2. Discover Agents

```csharp
using Aevatar.GAgents.AI.Common;

// Scan for agents
var agents = SimpleAgentScanner.ScanAllLoadedAssemblies();

// Use in your HTTP service
foreach (var agent in agents)
{
    Console.WriteLine($"Found: {agent.Name} - {agent.L1Description}");
}
```

## 📊 Features

- **Automatic Agent Discovery**: Scan assemblies for marked Agent classes
- **Standardized Metadata**: Extract consistent Agent information for LLM consumption
- **Performance Optimized**: Fast scanning with caching support
- **Testing Framework**: Complete unit test coverage
- **HTTP Integration**: Ready for REST API exposure

## 📋 Core Components

- `AgentDescriptionAttribute` - Mark Agent classes with metadata
- `AgentIndexInfo` - Standardized Agent information structure
- `SimpleAgentScanner` - Assembly scanning and Agent discovery
- Comprehensive unit tests with real Agent classes

## 📖 Documentation

**📘 [Complete Integration Guide](../../md/agent-information-management-guide.md)**

Includes:
- Detailed usage examples
- HTTP service integration patterns
- LLM integration strategies
- Best practices and troubleshooting
- Performance optimization tips

## 🧪 Testing

```bash
cd test/Aevatar.GAgents.AI.Abstractions.Test
dotnet test
```

7 test cases covering:
- Agent discovery functionality
- Data extraction and mapping
- Multiple agent handling
- Performance validation
- Boundary condition testing

## 🔧 Integration Examples

### HTTP Service
```csharp
public class AgentDiscoveryService
{
    private readonly List<AgentIndexInfo> _agents;
    
    public AgentDiscoveryService()
    {
        _agents = SimpleAgentScanner.ScanAllLoadedAssemblies();
    }
    
    public List<AgentIndexInfo> GetAllAgents() => _agents;
}
```

### ASP.NET Core API
```csharp
[HttpGet("agents")]
public ActionResult<List<AgentIndexInfo>> GetAgents()
{
    return _agentService.GetAllAgents();
}
```

## ✅ Current Status

- ✅ Core infrastructure implemented
- ✅ Unit tests passing (7/7)
- ✅ SocialGAgent and RouterGAgent marked as examples
- ✅ Performance validated (1ms for 3 agents)
- ✅ Ready for Agent rollout

## 🎯 Next Steps

1. Mark additional Agent classes with `AgentDescriptionAttribute`
2. Integrate into HTTP services for LLM consumption
3. Implement L1-L2 dual-layer filtering for token optimization
4. Add semantic similarity matching for enhanced Agent selection

---

**For detailed integration instructions, see the [Integration Guide](../../md/agent-information-management-guide.md)**
