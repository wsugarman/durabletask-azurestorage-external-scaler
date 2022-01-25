// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider
{
    internal class TaskHubBrowser : ITaskHubBrowser
    {
        private readonly ILogger<TaskHubBrowser> _logger;

        public TaskHubBrowser(ILogger<TaskHubBrowser> logger)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async ValueTask<TaskHubInfo> GetAsync(BlobServiceClient client, string taskHubName, CancellationToken cancellationToken = default)
        {
            if (client is null)
                throw new ArgumentNullException(nameof(client));

            if (string.IsNullOrEmpty(taskHubName))
                throw new ArgumentNullException(nameof(taskHubName));

            BlobContainerClient containerClient = client.GetBlobContainerClient(AzureStorageProvider.GetLeaseContainerName(taskHubName));
            BlobClient blobClient = containerClient.GetBlobClient(AzureStorageProvider.TaskHubBlobName);

            try
            {
                Stream blob = await blobClient.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                await using (blob.ConfigureAwait(false))
                {
                    return await JsonSerializer.DeserializeAsync<TaskHubInfo>(blob, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Cannot find Task Hub lease '{LeaseName}' for Task Hub '{TaskHubName}' in Azure Storage Account '{AccountName}'.",
                    containerClient.Name,
                    taskHubName,
                    client.AccountName);

                return default;
            }
        }
    }
}
