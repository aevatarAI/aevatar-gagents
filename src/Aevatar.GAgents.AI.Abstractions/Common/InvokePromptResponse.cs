using System.Collections.Generic;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.AI.Common;

public class InvokePromptResponse<T> where T : StateLogEventBase<T>
{
    public List<ChatMessage> ChatReponseList { get; set; }
    public TokenUsage<T> TokenUsage { get; set; }
}