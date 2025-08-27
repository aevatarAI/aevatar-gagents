using System.Threading.Tasks;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.ConfigValidateGagent.GAgents.ConfigValidateGAgent;

/// <summary>
/// Interface for Configuration Validation GAgent that demonstrates config validation capabilities
/// </summary>
public interface IConfigValidateGAgent : IStateGAgent<ConfigValidateGAgentState>
{
    /// <summary>
    /// Get the current validation status
    /// </summary>
    /// <returns>Validation status</returns>
    Task<string> GetValidationStatusAsync();
}