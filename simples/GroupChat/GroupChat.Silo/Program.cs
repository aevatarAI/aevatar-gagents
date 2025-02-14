﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aevatar.Core.Abstractions;


var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(silo =>
    {
        silo.AddMemoryGrainStorage("Default")
            .AddMemoryStreams(AevatarCoreConstants.StreamProvider)
            .AddMemoryGrainStorage("PubSubStore")
            .AddLogStorageBasedLogConsistencyProvider("LogStorage")
            .UseLocalhostClustering()
            .ConfigureLogging(logging => logging.AddConsole());
    })
    .UseConsoleLifetime();

using var host = builder.Build();

await host.RunAsync();