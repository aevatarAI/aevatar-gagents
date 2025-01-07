
using Volo.Abp.Modularity;


namespace AISmart;

public abstract class AISmartOrleansTestBase<TStartupModule> : 
    AISmartTestBase<TStartupModule> where TStartupModule : IAbpModule
{

    protected readonly TestCluster Cluster;

    protected AISmartOrleansTestBase() 
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}