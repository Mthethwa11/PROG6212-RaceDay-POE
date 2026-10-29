using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Services
{
    /// <summary>Thrown when an upload is attempted but no storage connection string is configured.</summary>
    public class BlobStorageNotConfiguredException : Exception
    {
        public BlobStorageNotConfiguredException()
            : base("Azure Blob Storage is not configured (AzureBlob:ConnectionString is missing).") { }
    }

    /// <summary>
    /// All Azure Blob Storage access lives behind this interface, inside the API.
    /// The MVC app never talks to Azure; it sends files to the API, which stores them here.
    /// </summary>
    public interface IBlobStorageService
    {
        string EventBannersContainer { get; }
        string ProfilePicturesContainer { get; }

        /// <summary>Uploads a file and returns its public URL.</summary>
        Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>Deletes a blob given its URL. Best effort: never throws if it can't delete.</summary>
        Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly string? _connectionString;
        private BlobServiceClient? _client;

        public BlobStorageService(IConfiguration configuration)
        {
            // The connection string comes from User Secrets locally, or an environment variable
            // (AzureBlob__ConnectionString) elsewhere. It is never stored in the repository.
            _connectionString = configuration["AzureBlob:ConnectionString"];
            EventBannersContainer = configuration["AzureBlob:EventBannersContainer"] ?? "event-banners";
            ProfilePicturesContainer = configuration["AzureBlob:ProfilePicturesContainer"] ?? "profile-pictures";
        }

        public string EventBannersContainer { get; }
        public string ProfilePicturesContainer { get; }

        // Created on first use, so the API (and the unit tests) start fine without a connection string.
        private BlobServiceClient Client
        {
            get
            {
                if (_client == null)
                {
                    if (string.IsNullOrWhiteSpace(_connectionString))
                    {
                        throw new BlobStorageNotConfiguredException();
                    }
                    _client = new BlobServiceClient(_connectionString);
                }
                return _client;
            }
        }

        public async Task<string> UploadAsync(string containerName, string blobName, Stream content,
            string contentType, CancellationToken cancellationToken = default)
        {
            var container = Client.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobName);

            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            }, cancellationToken);

            return blob.Uri.ToString();
        }

        public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri)) return;

                // Only ever delete blobs that belong to our own storage account.
                if (!string.Equals(uri.Host, Client.Uri.Host, StringComparison.OrdinalIgnoreCase)) return;

                var parts = new BlobUriBuilder(uri);
                var container = Client.GetBlobContainerClient(parts.BlobContainerName);
                await container.DeleteBlobIfExistsAsync(parts.BlobName, cancellationToken: cancellationToken);
            }
            catch (RequestFailedException)
            {
                // Leaving an old image behind is harmless; don't fail the request over it.
            }
            catch (BlobStorageNotConfiguredException)
            {
            }
        }
    }
}
