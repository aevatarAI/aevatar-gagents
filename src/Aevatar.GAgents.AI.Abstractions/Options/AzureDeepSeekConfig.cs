using System.ComponentModel.DataAnnotations;

namespace Aevatar.GAgents.AI.Options;

public class AzureDeepSeekConfig
{
    public const string ConfigSectionName = "DeepSeek";

    [Required]
    public string ModelId { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string? OrgId { get; set; } = null;
}