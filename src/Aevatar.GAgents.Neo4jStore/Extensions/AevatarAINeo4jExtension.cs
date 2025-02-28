using Aevatar.GAgents.GraphRag.Abstractions;
using Aevatar.GAgents.Neo4jStore.GraphRagStore;
using Aevatar.GAgents.Neo4jStore.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Aevatar.GAgents.Neo4jStore.Extensions;

public static class AevatarAiNeo4JStoreExtension
{
    public static IServiceCollection AddNeo4JStore(this IServiceCollection services)
    {
        services.AddSingleton<IDriver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Neo4jDriverConfig>>().Value;
            return GraphDatabase.Driver(
                options.Uri,
                AuthTokens.Basic(
                    options.User,
                    options.Password
                ));
        });
        
        services.AddSingleton<IGraphRagStore, Neo4JStore>();
        
        return services;
    }
}