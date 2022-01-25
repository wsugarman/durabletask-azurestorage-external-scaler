// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Keda.Scaler.DurableTask.AzureStorage
{
    internal readonly struct QueueMetrics : IEquatable<QueueMetrics>
    {
        public int Length { get; init; }

        public TimeSpan DequeueLatency { get; init; }

        public bool HasActivity => DequeueLatency == TimeSpan.Zero;

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is QueueMetrics other && Equals(other);

        public bool Equals(QueueMetrics other)
            => Length == other.Length && DequeueLatency == other.DequeueLatency;

        public override int GetHashCode()
            => HashCode.Combine(Length, DequeueLatency);

        public static bool operator ==(QueueMetrics left, QueueMetrics right)
            => left.Equals(right);

        public static bool operator !=(QueueMetrics left, QueueMetrics right)
            => !left.Equals(right);
    }
}
