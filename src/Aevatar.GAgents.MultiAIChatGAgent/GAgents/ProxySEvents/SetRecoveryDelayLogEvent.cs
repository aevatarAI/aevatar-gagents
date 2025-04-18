namespace Aevatar.GAgents.MultiAIChatGAgent.GAgents.ProxySEvents;

[GenerateSerializer]
public class SetRecoveryDelayLogEvent : AIAgentStatusProxyLogEvent
{
    [Id(0)] public TimeSpan RecoveryDelay { get; set; }
}