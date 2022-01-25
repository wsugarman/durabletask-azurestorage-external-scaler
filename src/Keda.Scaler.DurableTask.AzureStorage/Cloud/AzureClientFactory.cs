// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;

namespace Keda.Scaler.DurableTask.AzureStorage.Cloud
{
    internal class AzureClientFactory
    {
        private readonly BlobClientOptions _blobClientOptions;
        private readonly QueueClientOptions _queueClientOptions;

        public AzureClientFactory(IOptionsSnapshot<BlobClientOptions> blobClientOptions, IOptionsSnapshot<QueueClientOptions> queueClientOptions)
        {
            _blobClientOptions = blobClientOptions?.Value ?? throw new ArgumentNullException(nameof(blobClientOptions));
            _queueClientOptions = queueClientOptions?.Value ?? throw new ArgumentNullException(nameof(queueClientOptions));
        }

        public BlobServiceClient GetBlobServiceClient(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            return new BlobServiceClient(connectionString, _blobClientOptions);
        }

        public BlobServiceClient GetBlobServiceClient(string accountName, CloudEndpoints endpoints)
        {
            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException(nameof(accountName));

            if (endpoints is null)
                throw new ArgumentNullException(nameof(endpoints));

            // We assume there is only 1 managed identity present in the hosting environment
            TokenCredential credential = new ManagedIdentityCredential(options: new TokenCredentialOptions { AuthorityHost = endpoints.AuthorityHost });
            return new BlobServiceClient(CreateServiceEndpoint("https", accountName, "blob." + endpoints.StorageSuffix), credential, _blobClientOptions);
        }

        public QueueServiceClient GetQueueServiceClient(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            return new QueueServiceClient(connectionString, _queueClientOptions);
        }

        public QueueServiceClient GetQueueServiceClient(string accountName, CloudEndpoints endpoints)
        {
            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException(nameof(accountName));

            if (endpoints is null)
                throw new ArgumentNullException(nameof(endpoints));

            // We assume there is only 1 managed identity present in the hosting environment
            TokenCredential credential = new ManagedIdentityCredential(options: new TokenCredentialOptions { AuthorityHost = endpoints.AuthorityHost });
            return new QueueServiceClient(CreateServiceEndpoint("https", accountName, "queue." + endpoints.StorageSuffix), credential, _queueClientOptions);
        }

        private static Uri CreateServiceEndpoint(string scheme, string accountName, string suffix)
            => new Uri($"{scheme}://{accountName}.{suffix}", UriKind.Absolute);
    }
}
