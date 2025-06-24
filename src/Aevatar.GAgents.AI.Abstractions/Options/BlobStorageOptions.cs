using System.ComponentModel.DataAnnotations;

namespace Aevatar.GAgents.AI.Options;

/// <summary>
/// Configuration options for blob storage providers
/// </summary>
public class BlobStorageOptions
{
    /// <summary>
    /// Maximum size for a single image in bytes (default: 10MB)
    /// </summary>
    public long MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// S3 specific configuration
    /// </summary>
    public S3BlobOptions S3 { get; set; } = new();
}

/// <summary>
/// S3 specific blob storage configuration
/// </summary>
public class S3BlobOptions
{
    /// <summary>
    /// AWS Access Key ID
    /// </summary>
    [Required]
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS Secret Access Key
    /// </summary>
    [Required]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// S3 Bucket name
    /// </summary>
    [Required]
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// AWS Region
    /// </summary>
    [Required]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Optional S3 endpoint URL for custom S3-compatible services
    /// </summary>
    public string? EndpointUrl { get; set; }
}