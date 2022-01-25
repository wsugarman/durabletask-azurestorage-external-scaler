// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Common
{
    internal sealed class Clock
    {
        public static IClock System { get; } = new SystemClock();

        private sealed class SystemClock : IClock
        {
            public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        }
    }
}
