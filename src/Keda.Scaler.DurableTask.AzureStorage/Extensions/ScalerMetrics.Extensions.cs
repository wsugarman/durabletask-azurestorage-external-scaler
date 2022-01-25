// Copyright © William Sugarman.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions
{
    internal static class ScalerMetricsExtensions
    {
        public static IEnumerable<MetricSpec> ToMetricSpecs(this ScalerMetrics metrics)
            => metrics.ToEnumerable().Select(x => new MetricSpec { MetricName = x.Name, TargetSize = x.Value });

        public static IEnumerable<MetricValue> ToMetricValues(this ScalerMetrics metrics)
            => metrics.ToEnumerable().Select(x => new MetricValue { MetricName = x.Name, MetricValue_ = x.Value });

        private static IEnumerable<(string Name, long Value)> ToEnumerable(this ScalerMetrics metrics)
        {
            yield return (nameof(ScalerMetrics.ControlMessageLatency), (long)metrics.ControlMessageLatency.TotalMilliseconds);
            yield return (nameof(ScalerMetrics.ControlQueueLength), metrics.ControlQueueLength);
            yield return (nameof(ScalerMetrics.WorkerDemand), metrics.WorkerDemand);
            yield return (nameof(ScalerMetrics.WorkItemLatency), (long)metrics.WorkItemLatency.TotalMilliseconds);
            yield return (nameof(ScalerMetrics.WorkItemQueueLength), metrics.WorkItemQueueLength);
        }
    }
}
