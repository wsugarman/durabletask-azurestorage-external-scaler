// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage
{
    public readonly struct ScalerMetrics : IEquatable<ScalerMetrics>
    {
        public int ControlQueueDemand { get; init; }

        public long ControlQueueLatencyMs { get; init; }

        public long WorkItemQueueLatencyMs { get; init; }

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is ScalerMetrics other && Equals(other);

        public bool Equals(ScalerMetrics other)
            => ControlQueueDemand == other.ControlQueueDemand
            && ControlQueueLatencyMs == other.ControlQueueLatencyMs
            && WorkItemQueueLatencyMs == other.WorkItemQueueLatencyMs;

        public override int GetHashCode()
            => HashCode.Combine(
                ControlQueueDemand,
                ControlQueueLatencyMs,
                WorkItemQueueLatencyMs);

        public static bool operator ==(ScalerMetrics left, ScalerMetrics right)
            => left.Equals(right);

        public static bool operator !=(ScalerMetrics left, ScalerMetrics right)
            => !left.Equals(right);
    }
}
