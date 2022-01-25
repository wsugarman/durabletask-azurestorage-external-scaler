// Copyright © William Sugarman.
// Licensed under the MIT License.

using Azure;

namespace Keda.Scaler.DurableTask.AzureStorage.Extensions
{
    internal static class ResponseExtensions
    {
        public static bool IsSuccessStatusCode<T>(this Response<T> response)
            => response is not null && IsSuccessStatusCode(response.GetRawResponse().Status);

        private static bool IsSuccessStatusCode(int statusCode)
            => statusCode >= 200 && statusCode < 300;
    }
}
