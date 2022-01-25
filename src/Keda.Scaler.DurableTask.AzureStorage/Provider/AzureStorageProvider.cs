// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Keda.Scaler.DurableTask.AzureStorage.Provider
{
    internal static class AzureStorageProvider
    {
        public const string TaskHubBlobName = "taskhub.json";

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "DurableTask framework normalizes to lowercase.")]
        public static string GetControlQueueName(string taskHubName, int partition)
            => !string.IsNullOrEmpty(taskHubName)
            ? taskHubName.ToLowerInvariant() + "-control-" + partition.ToString("00", CultureInfo.InvariantCulture)
            : throw new ArgumentNullException(nameof(taskHubName));

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "DurableTask framework normalizes to lowercase.")]
        public static string GetLeaseContainerName(string taskHubName)
            => !string.IsNullOrEmpty(taskHubName)
            ? taskHubName.ToLowerInvariant() + "-leases"
            : throw new ArgumentNullException(nameof(taskHubName));

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "DurableTask framework normalizes to lowercase.")]
        public static string GetWorkItemQueueName(string taskHubName)
            => !string.IsNullOrEmpty(taskHubName)
            ? taskHubName.ToLowerInvariant() + "-workitems"
            : throw new ArgumentNullException(nameof(taskHubName));
    }
}
