﻿using AISmart.GAgent.Telegram.Options;
using AISmart.GAgent.Telegram.Provider;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Telegram;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AISmartGAgentTelegramModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddSingleton<ITelegramProvider, TelegramProvider>();
        // Configure<TelegramOptionsDto>(configuration.GetSection("Telegram")); 
    }
}