using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AISmart.Mock;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Volo.Abp.AutoMapper;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;

public class ClusterFixture : IDisposable, ISingletonDependency
{
    public static MockLoggerProvider LoggerProvider { get; set; }

    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }

    private class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder hostBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.secrets.json")
                .Build();
            
            hostBuilder.ConfigureServices(services =>
            {
                services.AddAutoMapper(typeof(AIApplicationGrainsModule).Assembly);
                services.AddAutoMapper(typeof(AISmartCQRSAutoMapperProfile).Assembly);
                var mock = new Mock<ILocalEventBus>();
                services.AddSingleton(typeof(ILocalEventBus), mock.Object);

                // Configure logging
                var loggerProvider = new MockLoggerProvider("AISmart");
                services.AddSingleton<ILoggerProvider>(loggerProvider);
                LoggerProvider = loggerProvider;
                services.AddLogging(logging =>
                {
                    //logging.AddProvider(loggerProvider);
                    logging.AddConsole(); // Adds console logger
                });
                var aelfNodeProvider = new MockAElfNodeProvider();
                services.AddSingleton<IAElfNodeProvider>(aelfNodeProvider);
                services.OnExposing(onServiceExposingContext =>
                {
                    var implementedTypes = ReflectionHelper.GetImplementedGenericTypes(
                        onServiceExposingContext.ImplementationType,
                        typeof(IObjectMapper<,>)
                    );
                });

                services.AddTransient(typeof(IObjectMapper<>), typeof(DefaultObjectMapper<>));
                services.AddTransient(typeof(IObjectMapper), typeof(DefaultObjectMapper));
                services.AddTransient(typeof(IAutoObjectMappingProvider), typeof(AutoMapperAutoObjectMappingProvider));
                services.AddTransient(sp => new MapperAccessor()
                {
                    Mapper = sp.GetRequiredService<IMapper>()
                });
                //services.AddMediatR(typeof(TestSiloConfigurations).Assembly);

                services.AddTransient<IMapperAccessor>(provider => provider.GetRequiredService<MapperAccessor>());
                services.AddMediatR(typeof(SaveStateCommandHandler).Assembly);
                services.AddMediatR(typeof(GetStateQueryHandler).Assembly);
                services.AddMediatR(typeof(SendEventCommandHandler).Assembly);
                services.AddMediatR(typeof(SaveGEventCommandHandler).Assembly);
                services.AddMediatR(typeof(GetGEventQueryHandler).Assembly);
                services.AddMediatR(typeof(SaveLogCommandHandler).Assembly);
                services.AddMediatR(typeof(GetLogQueryHandler).Assembly);

                services.AddTransient<SaveStateCommandHandler>();
                services.AddTransient<GetGEventQueryHandler>();
                services.AddTransient<SendEventCommandHandler>();
                services.AddTransient<SaveGEventCommandHandler>();
                services.AddSingleton<IIndexingService, ElasticIndexingService>();
                services.AddTransient<SaveLogCommandHandler>();
                services.AddTransient<GetLogQueryHandler>();

                services.AddSingleton(typeof(IEventDispatcher), typeof(CQRSProvider));
                services.AddSingleton(typeof(ICQRSProvider), typeof(CQRSProvider));
                /*var mockElasticClient = new Mock<IElasticClient>();
                services.AddSingleton(mockElasticClient.Object);
                var _mockIndexingService = new Mock<IIndexingService>();
                services.AddSingleton(_mockIndexingService.Object); */
                services.AddSingleton<IElasticClient>(provider =>
                {
                    var settings =new ConnectionSettings(new Uri("http://127.0.0.1:9200"))
                        .DefaultIndex("cqrs");
                    return new ElasticClient(settings);
                });
                services.AddSingleton(typeof(ICqrsService), typeof(CqrsService));
                
                services.AddSingleton(typeof(INameContestProvider), typeof(NameContestProvider));
            })
            .AddMemoryStreams("AISmart")
            .AddMemoryGrainStorage("PubSubStore")
            .AddMemoryGrainStorageAsDefault()
            .AddLogStorageBasedLogConsistencyProvider("LogStorage")
            .Configure<MicroAIOptions>(configuration.GetSection("AutogenConfig"))
            .Configure<NameContestOptions>(configuration.GetSection("NameContest"));
        }
    }

    public class MapperAccessor : IMapperAccessor
    {
        public IMapper Mapper { get; set; }
    }

    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddMemoryStreams("AISmart");
    }
    
    public static async Task WaitLogAsync(string log)
    {
        var timeout = TimeSpan.FromSeconds(15);
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (LoggerProvider.Logs.Any(l => l.Contains(log)))
            {
                break;
            }
            await Task.Delay(1000);
        }
    }
}