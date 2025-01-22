using System.Linq.Expressions;
using Aevatar.Core.Abstractions;
using Orleans.TestKit;

namespace Aevatar.GAgent.MicroAI.Tests;

public abstract class GAgentTestKitBase : TestKitBase
{
    protected void AddProbesByGrainId(params IGAgent?[] gAgents)
    {
        foreach (var gAgent in gAgents)
        {
            Silo.AddProbe(gAgent.GetGrainId(), gAgent);
        }
    }

    protected void AddProbesByIdSpan(params IGAgent?[] gAgents)
    {
        var parameter = Expression.Parameter(typeof(IdSpan), "idSpan");
        Expression body = Expression.Constant(null, typeof(IGAgent));

        foreach (var gAgent in gAgents)
        {
            var primaryKey = gAgent.GetPrimaryKey();
            var grainId = GrainIdKeyExtensions.CreateGuidKey(primaryKey);
            var condition = Expression.Equal(parameter, Expression.Constant(grainId));
            var result = Expression.Constant(gAgent, typeof(IGAgent));
            body = Expression.Condition(condition, result, body);
        }

        var lambda = Expression.Lambda<Func<IdSpan, IGAgent>>(body, parameter).Compile();
        Silo.AddProbe(lambda);
    }
}