using System;
using Aevatar.GAgents.AI.Brain;

namespace Aevatar.GAgents.AI.BrainFactory;

public interface IBrainFactory
{
    IBrain? GetBrain(string llm);
}