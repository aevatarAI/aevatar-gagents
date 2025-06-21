using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Aevatar.GAgents.AI.Storage;

/// <summary>
/// 云Blob存储服务抽象接口
/// 提供统一的图片存储、获取和删除功能
/// </summary>
public interface IBlobStorageService : ITransientDependency
{
    /// <summary>
    /// 根据Blob ID获取图片数据
    /// </summary>
    /// <param name="blobId">图片的Blob ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片的字节数组</returns>
    Task<byte[]> GetImageAsync(string blobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传图片数据到云存储
    /// </summary>
    /// <param name="imageData">图片字节数组</param>
    /// <param name="fileName">文件名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传后的Blob ID</returns>
    Task<string> UploadImageAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定的图片
    /// </summary>
    /// <param name="blobId">要删除的图片Blob ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除是否成功</returns>
    Task<bool> DeleteImageAsync(string blobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证图片格式和大小
    /// </summary>
    /// <param name="imageData">图片字节数组</param>
    /// <param name="fileName">文件名</param>
    /// <returns>验证是否通过</returns>
    Task<bool> ValidateImageAsync(byte[] imageData, string fileName);
} 