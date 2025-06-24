using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// Abstract interface for blob storage providers
/// </summary>
public interface IBlobStorageProvider
{
    Task<Blob> DownloadAsync(string key, CancellationToken cancellationToken = default);
    
    Task UploadAsync(string key, Stream stream, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

public class Blob
{
    public byte[] Bytes { get; set; }
    public string MimeType { get; set; }
}