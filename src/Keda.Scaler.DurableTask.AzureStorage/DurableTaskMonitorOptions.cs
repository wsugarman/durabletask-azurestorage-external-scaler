// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage
{
    internal sealed class DurableTaskMonitorOptions
    {
        public string TaskHubName { get; set; } = "TestHubName";

        public TimeSpan MaxQueuePollingInterval { get; set; } = TimeSpan.FromSeconds(30);
    }
}
