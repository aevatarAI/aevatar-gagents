namespace Aevatar.GAgents.AI.Storage;

/// <summary>
/// AWS Blob存储服务接口
/// 继承通用IBlobStorageService，提供AWS S3特定实现
/// </summary>
public interface IAWSBlobStorageService : IBlobStorageService
{
    /// <summary>
    /// 获取S3存储桶名称
    /// </summary>
    string BucketName { get; }

    /// <summary>
    /// 获取AWS区域
    /// </summary>
    string Region { get; }
} 