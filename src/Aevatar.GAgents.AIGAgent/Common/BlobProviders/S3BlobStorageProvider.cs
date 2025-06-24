using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.S3.Model;

namespace Aevatar.AI.Common.BlobProviders;

public class S3BlobStorageProvider: IBlobStorageProvider, IDisposable
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<S3BlobStorageProvider> _logger;
    private readonly IAmazonS3 _s3Client;

    public S3BlobStorageProvider(IOptions<BlobStorageOptions> options, ILogger<S3BlobStorageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        // Initialize S3 client with credentials from configuration
        if (_options.S3 != null)
        {
            var config = new AmazonS3Config();
            
            if (!string.IsNullOrWhiteSpace(_options.S3.Region))
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.S3.Region);
            }
            
            if (!string.IsNullOrWhiteSpace(_options.S3.EndpointUrl))
            {
                config.ServiceURL = _options.S3.EndpointUrl;
                config.ForcePathStyle = true; // Required for custom endpoints like MinIO
            }

            if (!string.IsNullOrWhiteSpace(_options.S3.AccessKey) && 
                !string.IsNullOrWhiteSpace(_options.S3.SecretKey))
            {
                _s3Client = new AmazonS3Client(_options.S3.AccessKey, _options.S3.SecretKey, config);
            }
            else
            {
                // Use default credentials (IAM role, environment variables, etc.)
                _s3Client = new AmazonS3Client(config);
            }
            
            _logger.LogInformation("S3 client initialized for bucket: {BucketName}, region: {Region}", 
                _options.S3.BucketName, _options.S3.Region);
        }
        else
        {
            throw new InvalidOperationException("S3 configuration is required but not provided");
        }
    }

    public async Task<Blob> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Empty or null key provided");
            return null;
        }

        _logger.LogInformation("Downloading from S3: {Key} from bucket: {BucketName}",
            key, _options.S3.BucketName);

        var request = new GetObjectRequest
        {
            BucketName = _options.S3.BucketName,
            Key = key
        };

        using var response = await _s3Client.GetObjectAsync(request, cancellationToken);

        var bytes = await response.ResponseStream.GetAllBytesAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Successfully downloaded {Key}, size: {Size} bytes",
            key, bytes.Length);

        return new Blob
        {
            Bytes = bytes,
            MimeType = response.Headers.ContentType
        };
    }

    public async Task UploadAsync(string key, Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream.Length > _options.MaxImageSizeBytes)
        {
            throw new ArgumentException("File too large.");
        }

        var putObjectRequest = new PutObjectRequest
        {
            InputStream = stream,
            BucketName = _options.S3.BucketName,
            Key = key
        };
        await _s3Client.PutObjectAsync(putObjectRequest, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var deleteObjectRequest = new DeleteObjectRequest()
        {
            BucketName = _options.S3.BucketName,
            Key = key
        };
        await _s3Client.DeleteObjectAsync(deleteObjectRequest, cancellationToken);
    }

    /// <summary>
    /// Disposes the S3 client and releases resources
    /// </summary>
    public void Dispose()
    {
        _s3Client?.Dispose();
    }
} 