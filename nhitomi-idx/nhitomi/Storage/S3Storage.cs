using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace nhitomi.Storage
{
    public class S3StorageOptions
    {
        /// <summary>
        /// AWS access key.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// AWS secret key.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Name of S3 bucket.
        /// </summary>
        public string BucketName { get; set; }

        /// <inheritdoc cref="ClientConfig.MaxErrorRetry"/>
        public int MaxErrorRetry { get; set; } = 5;

        /// <inheritdoc cref="AmazonS3Config.UseAccelerateEndpoint"/>
        public bool UseAccelerateEndpoint { get; set; }

        /// <inheritdoc cref="ClientConfig.RegionEndpoint"/>
        public string Region { get; set; }

        /// <inheritdoc cref="ClientConfig.ServiceURL"/>
        public string ServiceUrl { get; set; }
    }

    public class S3AspNetHttpClientFactoryWrapper : HttpClientFactory
    {
        readonly IHttpClientFactory _factory;
        readonly string _name;

        public S3AspNetHttpClientFactoryWrapper(IHttpClientFactory factory, string name)
        {
            _factory = factory;
            _name    = name;
        }

        public override HttpClient CreateHttpClient(IClientConfig clientConfig) => _factory.CreateClient(_name);
    }

    public class S3Storage : IStorage
    {
        readonly S3StorageOptions _options;
        readonly ILogger<S3Storage> _logger;
        readonly IAmazonS3 _client;

        public S3Storage(S3StorageOptions options, ILogger<S3Storage> logger, IHttpClientFactory http)
        {
            _options = options;
            _logger  = logger;

            if (options.AccessKey == null || options.SecretKey == null)
                throw new ArgumentException("AWS S3 access key and secret key must be configured.");

            if (options.Region == null || options.ServiceUrl == null)
                throw new ArgumentException("AWS S3 region must be configured.");

            _client = new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), new AmazonS3Config
            {
                BufferSize            = 1684,
                RetryMode             = RequestRetryMode.Standard,
                HttpClientFactory     = new S3AspNetHttpClientFactoryWrapper(http, nameof(S3Storage)),
                MaxErrorRetry         = options.MaxErrorRetry,
                UseAccelerateEndpoint = options.UseAccelerateEndpoint,
                RegionEndpoint        = RegionEndpoint.GetBySystemName(options.Region)
            }.Chain(c =>
            {
                if (options.ServiceUrl != null)
                    c.ServiceURL = options.ServiceUrl;
            }));
        }

        Task IStorage.InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.GetObjectAsync(_options.BucketName, name, cancellationToken);

                return new StorageFile
                {
                    Name      = response.Key,
                    MediaType = response.Headers.ContentType,
                    Stream    = response.ResponseStream
                };
            }
            catch (AmazonS3Exception e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                    return new NotFound();

                _logger.LogWarning(e, $"Failed to read file '{name}'.");
                return e;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to read file '{name}'.");
                return e;
            }
        }

        public async Task<OneOf<Success, Exception>> WriteAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName  = _options.BucketName,
                    Key         = file.Name,
                    ContentType = file.MediaType,
                    InputStream = file.Stream
                };

                await _client.PutObjectAsync(request, cancellationToken);

                _logger.LogInformation($"Wrote file '{file.Name}'.");

                return new Success();
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to write file '{file.Name}'.");
                return e;
            }
        }

        public async Task DeleteAsync(string[] names, CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Quiet      = true,
                    Objects    = names.ToList(name => new KeyVersion { Key = name })
                }, cancellationToken);

                _logger.LogInformation($"Deleted files '{string.Join("', '", names)}'.");
            }
            catch (DeleteObjectsException e)
            {
                foreach (var error in e.Response.DeleteErrors)
                    _logger.LogInformation($"Ignored file delete failure '{error.Key}': {error.Message}");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to delete files '{string.Join("', '", names)}'.");
            }
        }

        public void Dispose() => _client.Dispose();
    }
}