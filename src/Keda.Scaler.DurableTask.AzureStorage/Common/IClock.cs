// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;

namespace Keda.Scaler.DurableTask.AzureStorage.Common
{
    internal interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
