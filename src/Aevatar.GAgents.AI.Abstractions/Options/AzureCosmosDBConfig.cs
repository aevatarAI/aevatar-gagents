﻿using System.ComponentModel.DataAnnotations;

namespace Aevatar.GAgents.AI.Options;

/// <summary>
/// Azure CosmosDB service settings for use with AzureCosmosDBMongoDB and AzureCosmosDBNoSQL.
/// </summary>
internal sealed class AzureCosmosDBConfig
{
    public const string MongoDBConfigSectionName = "AzureCosmosDBMongoDB";
    public const string NoSQLConfigSectionName = "AzureCosmosDBNoSQL";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;
}
