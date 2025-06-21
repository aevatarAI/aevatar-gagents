using Orleans;

namespace Aevatar.GAgents.AI.Options;

/// <summary>
/// Blob存储配置选项
/// </summary>
[GenerateSerializer]
public class BlobStorageOptions
{
    /// <summary>
    /// 存储提供商 (AWS, Azure, GCP)
    /// </summary>
    [Id(0)] public string Provider { get; set; } = "AWS";

    /// <summary>
    /// 连接字符串或配置
    /// </summary>
    [Id(1)] public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 存储桶/容器名称
    /// </summary>
    [Id(2)] public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 最大文件大小(MB)
    /// </summary>
    [Id(3)] public int MaxFileSizeMB { get; set; } = 10;

    /// <summary>
    /// 允许的文件类型
    /// </summary>
    [Id(4)] public string[] AllowedFileTypes { get; set; } = {"jpg", "jpeg", "png", "gif", "webp"};

    /// <summary>
    /// AWS 区域设置
    /// </summary>
    [Id(5)] public string? AWSRegion { get; set; }

    /// <summary>
    /// AWS 访问密钥ID
    /// </summary>
    [Id(6)] public string? AWSAccessKeyId { get; set; }

    /// <summary>
    /// AWS 秘密访问密钥
    /// </summary>
    [Id(7)] public string? AWSSecretAccessKey { get; set; }
} 