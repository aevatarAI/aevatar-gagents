using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.AI.Brain;

public interface IBrain
{
    Task InitializeAsync(string id, string description);

    Task<bool> UpsertKnowledgeAsync(List<BrainContent>? files = null);

    Task<List<ChatMessage>> InvokePromptAsync(string content, List<ChatMessage>? history = null,
        bool ifUseKnowledge = false);
}