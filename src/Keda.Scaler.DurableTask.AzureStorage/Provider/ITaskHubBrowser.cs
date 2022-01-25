// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider
{
    internal interface ITaskHubBrowser
    {
        ValueTask<TaskHubInfo> GetAsync(BlobServiceClient client, string taskHubName, CancellationToken cancellationToken = default);
    }
}
