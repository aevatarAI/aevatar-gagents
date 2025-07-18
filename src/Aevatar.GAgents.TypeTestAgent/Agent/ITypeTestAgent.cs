using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.TypeTestAgent.Dtos;

namespace Aevatar.GAgents.TypeTestAgent.Agent;

/// <summary>
/// Interface for TypeTest Agent that provides comprehensive type testing capabilities
/// </summary>
public interface ITypeTestAgent : IAIGAgent, IGAgent
{
    /// <summary>
    /// Apply type test configuration with all type examples
    /// </summary>
    Task<bool> ApplyTypeTestConfigAsync(TypeTestConfigDto config);
    
    /// <summary>
    /// Get current type test configuration as JSON
    /// </summary>
    Task<string> GetTypeTestConfigJsonAsync();
} 