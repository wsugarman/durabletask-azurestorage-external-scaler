// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Extensions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider
{
    internal sealed class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly IClock _clock;
        private readonly ILogger<PerformanceMonitor> _logger;

        public PerformanceMonitor(IClock clock, ILogger<PerformanceMonitor> logger)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async IAsyncEnumerable<QueueMetrics> GetControlMetricsAsync(QueueServiceClient client, TaskHubInfo taskHubInfo, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Start a task for fetching the metrics for each queue
            ValueTask<QueueMetrics>[] metricTasks = new ValueTask<QueueMetrics>[taskHubInfo.PartitionCount];
            for (int partition = 0; partition < taskHubInfo.PartitionCount; partition++)
            {
                QueueClient queueClient = client.GetQueueClient(AzureStorageProvider.GetControlQueueName(taskHubInfo.TaskHubName, partition));
#pragma warning disable CA2012 // ValueTasks are not being reused
                metricTasks[partition] = GetQueueMetricsAsync(queueClient, taskHubInfo.TaskHubName, cancellationToken);
#pragma warning restore CA2012
            }

            // Enumerate through the tasks
            for (int partition = 0; partition < taskHubInfo.PartitionCount; partition++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return await metricTasks[partition].ConfigureAwait(false);
            }
        }

        public ValueTask<QueueMetrics> GetWorkItemMetricsAsync(QueueServiceClient client, TaskHubInfo taskHubInfo, CancellationToken cancellationToken = default)
        {
            QueueClient queueClient = client.GetQueueClient(AzureStorageProvider.GetWorkItemQueueName(taskHubInfo.TaskHubName));
            return GetQueueMetricsAsync(queueClient, taskHubInfo.TaskHubName, cancellationToken);
        }

        private async ValueTask<QueueMetrics> GetQueueMetricsAsync(QueueClient client, string taskHubName, CancellationToken cancellationToken)
        {
            ValueTask<TimeSpan> latencyTask = GetDequeueLatencyAsync(client, taskHubName, cancellationToken);
            ValueTask<int> lengthTask = GetApproximateLengthAsync(client, taskHubName, cancellationToken);

            return new QueueMetrics
            {
                DequeueLatency = await latencyTask.ConfigureAwait(false),
                Length = await lengthTask.ConfigureAwait(false),
            };
        }

        private async ValueTask<TimeSpan> GetDequeueLatencyAsync(QueueClient client, string taskHubName, CancellationToken cancellationToken)
        {
            DateTimeOffset utcNow = _clock.UtcNow;
            try
            {
                Response<PeekedMessage> response = await client.PeekMessageAsync(cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode() && response.Value is not null)
                {
                    TimeSpan latency = utcNow - response.Value.InsertedOn.GetValueOrDefault(utcNow);
                    if (latency > TimeSpan.Zero)
                    {
                        return latency;
                    }
                }
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Cannot find queue '{QueueName}' for Task Hub '{TaskHubName}' in Azure Storage Account '{AccountName}'.",
                    client.Name,
                    taskHubName,
                    client.AccountName);
            }

            return TimeSpan.Zero;
        }

        private async ValueTask<int> GetApproximateLengthAsync(QueueClient client, string taskHubName, CancellationToken cancellationToken)
        {
            try
            {
                Response<QueueProperties> response = await client.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode() && response.Value is not null)
                {
                    return response.Value.ApproximateMessagesCount;
                }
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Cannot find queue '{QueueName}' for Task Hub '{TaskHubName}' in Azure Storage Account '{AccountName}'.",
                    client.Name,
                    taskHubName,
                    client.AccountName);
            }

            return 0;
        }
    }
}
