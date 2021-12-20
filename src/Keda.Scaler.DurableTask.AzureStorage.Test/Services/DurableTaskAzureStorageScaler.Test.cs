﻿// Copyright © William Sugarman.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.AzureStorage.Monitoring;
using k8s;
using k8s.Models;
using Keda.Scaler.DurableTask.AzureStorage.Common;
using Keda.Scaler.DurableTask.AzureStorage.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Keda.Scaler.DurableTask.AzureStorage.Services.Test
{
    [TestClass]
    public class DurableTaskAzureStorageScalerTest
    {
        [TestMethod]
        public void CtorExceptions()
        {
            IKubernetes kubernetes = Mock.Of<IKubernetes>();
            IPerformanceMonitorFactory monitorFactory = Mock.Of<IPerformanceMonitorFactory>();
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

            Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(null!, monitorFactory, loggerFactory));
            Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(kubernetes, null!, loggerFactory));
            Assert.ThrowsException<ArgumentNullException>(() => new DurableTaskAzureStorageScaler(kubernetes, monitorFactory, null!));
        }

        [TestMethod]
        public async Task GetMetricSpecAsync()
        {
            IKubernetes kubernetes = Mock.Of<IKubernetes>();
            IPerformanceMonitorFactory monitorFactory = Mock.Of<IPerformanceMonitorFactory>();
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(kubernetes, monitorFactory, loggerFactory);

            // Null metadata
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => scaler.GetMetricSpecAsync(null!, tokenSource.Token).AsTask()).ConfigureAwait(false);

            // Non-null metadata
            Assert.AreEqual(
                DurableTaskAzureStorageScaler.MetricSpecValue,
                await scaler.GetMetricSpecAsync(new ScalerMetadata(), tokenSource.Token).ConfigureAwait(false));
        }

        [DataTestMethod]
        [DataRow(ScaleAction.AddWorker, 2, 5, DurableTaskAzureStorageScaler.MetricSpecValue * 2 * 5)]
        [DataRow(ScaleAction.RemoveWorker, 2, 5, DurableTaskAzureStorageScaler.MetricSpecValue / 2 * 5)]
        [DataRow(ScaleAction.None, 2, 5, DurableTaskAzureStorageScaler.MetricSpecValue * 5)]
        public async Task GetMetricValueAsync(ScaleAction action, double ratio, int replicas, long expectedMetric)
        {
            // Create input
            DeploymentReference deployment = new DeploymentReference("unit-test-func", "durable-task");
            ScalerMetadata metadata = new ScalerMetadata
            {
                AccountName = "unitteststorage",
                MaxMessageLatencyMilliseconds = 1000,
                ScaleIncrement = ratio,
                TaskHubName = "UnitTestTaskHub",
                UseAAdPodIdentity = true,
            };

            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            // Create mock services
            V1Scale scale = new V1Scale { Status = new V1ScaleStatus { Replicas = replicas } };
            PerformanceHeartbeat heartbeat = CreateHeartbeat(action);

#pragma warning disable CA2000 // ReadNamespacedDeploymentScaleAsync will dipose
            Mock<IKubernetes> k8sMock = new Mock<IKubernetes>(MockBehavior.Strict);
            k8sMock
                .Setup(k => k.ReadNamespacedDeploymentScaleWithHttpMessagesAsync(deployment.Name, deployment.Namespace, null, null, tokenSource.Token))
                .ReturnsAsync(new HttpOperationResponse<V1Scale> { Body = scale });
#pragma warning restore CA2000

            Mock<IPerformanceMonitor> monitorMock = new Mock<IPerformanceMonitor>(MockBehavior.Strict);
            monitorMock
                .Setup(m => m.GetHeartbeatAsync(replicas))
                .ReturnsAsync(heartbeat);
            monitorMock
                .Setup(m => m.Dispose());

            Mock<IPerformanceMonitorFactory> factoryMock = new Mock<IPerformanceMonitorFactory>(MockBehavior.Strict);
            factoryMock
                .Setup(f => f.CreateAsync(metadata, tokenSource.Token))
                .ReturnsAsync(monitorMock.Object);

            IProcessEnvironment environment = CurrentEnvironment.Instance;
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

            DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(k8sMock.Object, factoryMock.Object, loggerFactory);

            // Null metadata
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => scaler.GetMetricValueAsync(default, null!).AsTask()).ConfigureAwait(false);

            // Non-null metadata
            Assert.AreEqual(expectedMetric, await scaler.GetMetricValueAsync(deployment, metadata, tokenSource.Token).ConfigureAwait(false));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task IsActiveAsync(bool isActive)
        {
            // Create input
            ScalerMetadata metadata = new ScalerMetadata
            {
                AccountName = "unitteststorage",
                MaxMessageLatencyMilliseconds = 1000,
                TaskHubName = "UnitTestTaskHub",
                UseAAdPodIdentity = true,
            };

            using CancellationTokenSource tokenSource = new CancellationTokenSource();

            // Create mock services
            PerformanceHeartbeat heartbeat = isActive
                ? new PerformanceHeartbeat
                {
                    ControlQueueLatencies = new TimeSpan[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1) },
                    WorkItemQueueLatency = TimeSpan.FromMilliseconds(300),
                }
                : new PerformanceHeartbeat
                {
                    ControlQueueLatencies = new TimeSpan[] { TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero },
                    WorkItemQueueLatency = TimeSpan.Zero,
                };

            Mock<IPerformanceMonitor> monitorMock = new Mock<IPerformanceMonitor>(MockBehavior.Strict);
            monitorMock
                .Setup(m => m.GetHeartbeatAsync(null))
                .ReturnsAsync(heartbeat);
            monitorMock
                .Setup(m => m.Dispose());

            Mock<IPerformanceMonitorFactory> factoryMock = new Mock<IPerformanceMonitorFactory>(MockBehavior.Strict);
            factoryMock
                .Setup(f => f.CreateAsync(metadata, tokenSource.Token))
                .ReturnsAsync(monitorMock.Object);

            IProcessEnvironment environment = CurrentEnvironment.Instance;
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance;

            DurableTaskAzureStorageScaler scaler = new DurableTaskAzureStorageScaler(Mock.Of<IKubernetes>(), factoryMock.Object, loggerFactory);

            // Null metadata
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => scaler.IsActiveAsync(null!, tokenSource.Token).AsTask()).ConfigureAwait(false);

            // Non-null metadata
            Assert.AreEqual(isActive, await scaler.IsActiveAsync(metadata, tokenSource.Token).ConfigureAwait(false));
        }

        private static PerformanceHeartbeat CreateHeartbeat(ScaleAction action, string? reason = null)
        {
            MethodInfo method = typeof(PerformanceHeartbeat)
                .GetProperty(nameof(PerformanceHeartbeat.ScaleRecommendation))!
                .GetSetMethod(nonPublic: true)!;

            ConstructorInfo ctor = typeof(ScaleRecommendation)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(ScaleAction), typeof(bool), typeof(string) })!;

            PerformanceHeartbeat heartbeat = new PerformanceHeartbeat();
            method.Invoke(heartbeat, new object[] { ctor.Invoke(new object?[] { action, false, reason }) });
            return heartbeat;
        }
    }
}