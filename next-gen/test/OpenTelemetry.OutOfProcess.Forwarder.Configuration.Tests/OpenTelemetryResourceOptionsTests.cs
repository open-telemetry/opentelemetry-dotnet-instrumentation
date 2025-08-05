// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

using OpenTelemetry.Configuration;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Configuration.Tests;

public sealed class OpenTelemetryResourceOptionsTests
{
    [Fact]
    public void ParseFromConfig_WithCompleteJsonConfiguration_ParsesAllResourceAttributes()
    {
        // Arrange - This JSON shows the complete structure for resource attributes configuration
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Resource": {
              "service.name": "my-application",
              "service.version": "1.0.0",
              "service.namespace": "production",
              "service.instance.id": "instance-1",
              "deployment.environment": "production",
              "deployment.environment.name": "prod-cluster-01",
              "cloud.provider": "aws",
              "cloud.platform": "aws_ec2",
              "cloud.region": "us-west-2",
              "cloud.availability_zone": "us-west-2a",
              "host.name": "web-server-01",
              "host.type": "virtual",
              "container.name": "my-app-container",
              "container.id": "abc123def456",
              "k8s.cluster.name": "production-cluster",
              "k8s.namespace.name": "default",
              "k8s.pod.name": "my-app-pod-xyz",
              "k8s.deployment.name": "my-app-deployment",
              "custom.team": "backend-team",
              "custom.cost_center": "engineering"
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var resourceSection = configuration.GetSection("OpenTelemetry:Resource");

        // Act
        var resourceOptions = OpenTelemetryResourceOptions.ParseFromConfig(resourceSection);

        // Assert - Verify service identification attributes
        Assert.NotNull(resourceOptions.AttributeOptions);
        Assert.Equal(20, resourceOptions.AttributeOptions.Count);

        var serviceNameAttr = resourceOptions.AttributeOptions.First(a => a.Key == "service.name");
        Assert.Equal("my-application", serviceNameAttr.ValueOrExpression);

        var serviceVersionAttr = resourceOptions.AttributeOptions.First(a => a.Key == "service.version");
        Assert.Equal("1.0.0", serviceVersionAttr.ValueOrExpression);

        var serviceNamespaceAttr = resourceOptions.AttributeOptions.First(a => a.Key == "service.namespace");
        Assert.Equal("production", serviceNamespaceAttr.ValueOrExpression);

        var serviceInstanceAttr = resourceOptions.AttributeOptions.First(a => a.Key == "service.instance.id");
        Assert.Equal("instance-1", serviceInstanceAttr.ValueOrExpression);

        // Verify deployment attributes
        var deploymentEnvAttr = resourceOptions.AttributeOptions.First(a => a.Key == "deployment.environment");
        Assert.Equal("production", deploymentEnvAttr.ValueOrExpression);

        var deploymentEnvNameAttr = resourceOptions.AttributeOptions.First(a => a.Key == "deployment.environment.name");
        Assert.Equal("prod-cluster-01", deploymentEnvNameAttr.ValueOrExpression);

        // Verify cloud provider attributes
        var cloudProviderAttr = resourceOptions.AttributeOptions.First(a => a.Key == "cloud.provider");
        Assert.Equal("aws", cloudProviderAttr.ValueOrExpression);

        var cloudPlatformAttr = resourceOptions.AttributeOptions.First(a => a.Key == "cloud.platform");
        Assert.Equal("aws_ec2", cloudPlatformAttr.ValueOrExpression);

        var cloudRegionAttr = resourceOptions.AttributeOptions.First(a => a.Key == "cloud.region");
        Assert.Equal("us-west-2", cloudRegionAttr.ValueOrExpression);

        var cloudAzAttr = resourceOptions.AttributeOptions.First(a => a.Key == "cloud.availability_zone");
        Assert.Equal("us-west-2a", cloudAzAttr.ValueOrExpression);

        // Verify host attributes
        var hostNameAttr = resourceOptions.AttributeOptions.First(a => a.Key == "host.name");
        Assert.Equal("web-server-01", hostNameAttr.ValueOrExpression);

        var hostTypeAttr = resourceOptions.AttributeOptions.First(a => a.Key == "host.type");
        Assert.Equal("virtual", hostTypeAttr.ValueOrExpression);

        // Verify container attributes
        var containerNameAttr = resourceOptions.AttributeOptions.First(a => a.Key == "container.name");
        Assert.Equal("my-app-container", containerNameAttr.ValueOrExpression);

        var containerIdAttr = resourceOptions.AttributeOptions.First(a => a.Key == "container.id");
        Assert.Equal("abc123def456", containerIdAttr.ValueOrExpression);

        // Verify Kubernetes attributes
        var k8sClusterAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.cluster.name");
        Assert.Equal("production-cluster", k8sClusterAttr.ValueOrExpression);

        var k8sNamespaceAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.namespace.name");
        Assert.Equal("default", k8sNamespaceAttr.ValueOrExpression);

        var k8sPodAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.pod.name");
        Assert.Equal("my-app-pod-xyz", k8sPodAttr.ValueOrExpression);

        var k8sDeploymentAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.deployment.name");
        Assert.Equal("my-app-deployment", k8sDeploymentAttr.ValueOrExpression);

        // Verify custom attributes
        var customTeamAttr = resourceOptions.AttributeOptions.First(a => a.Key == "custom.team");
        Assert.Equal("backend-team", customTeamAttr.ValueOrExpression);

        var customCostCenterAttr = resourceOptions.AttributeOptions.First(a => a.Key == "custom.cost_center");
        Assert.Equal("engineering", customCostCenterAttr.ValueOrExpression);
    }

    [Fact]
    public void ParseFromConfig_WithMinimalServiceConfiguration_ParsesBasicAttributes()
    {
        // Arrange - This JSON shows minimal resource configuration for a basic service
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Resource": {
              "service.name": "minimal-service",
              "service.version": "0.1.0"
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var resourceSection = configuration.GetSection("OpenTelemetry:Resource");

        // Act
        var resourceOptions = OpenTelemetryResourceOptions.ParseFromConfig(resourceSection);

        // Assert - Verify minimal configuration is parsed
        Assert.NotNull(resourceOptions.AttributeOptions);
        Assert.Equal(2, resourceOptions.AttributeOptions.Count);

        var serviceNameAttr = resourceOptions.AttributeOptions.First(a => a.Key == "service.name");
        Assert.Equal("minimal-service", serviceNameAttr.ValueOrExpression);

        var serviceVersionAttr = resourceOptions.AttributeOptions.First(a => a.Key == "service.version");
        Assert.Equal("0.1.0", serviceVersionAttr.ValueOrExpression);
    }

    [Fact]
    public void ParseFromConfig_WithKubernetesConfiguration_ParsesK8sAttributes()
    {
        // Arrange - This JSON demonstrates Kubernetes-specific resource configuration
        var jsonConfig = """
        {
          "OpenTelemetry": {
            "Resource": {
              "service.name": "k8s-microservice",
              "service.version": "2.1.0",
              "deployment.environment": "staging",
              "k8s.cluster.name": "staging-cluster",
              "k8s.namespace.name": "microservices",
              "k8s.pod.name": "microservice-pod-abc123",
              "k8s.deployment.name": "microservice-deployment",
              "k8s.replicaset.name": "microservice-rs-xyz789",
              "k8s.node.name": "worker-node-1"
            }
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonConfig)))
            .Build();
        var resourceSection = configuration.GetSection("OpenTelemetry:Resource");

        // Act
        var resourceOptions = OpenTelemetryResourceOptions.ParseFromConfig(resourceSection);

        // Assert - Verify Kubernetes configuration
        Assert.NotNull(resourceOptions.AttributeOptions);
        Assert.Equal(9, resourceOptions.AttributeOptions.Count);

        var serviceNameAttr = resourceOptions.AttributeOptions.First(a => a.Key == "service.name");
        Assert.Equal("k8s-microservice", serviceNameAttr.ValueOrExpression);

        var serviceVersionAttr = resourceOptions.AttributeOptions.First(a => a.Key == "service.version");
        Assert.Equal("2.1.0", serviceVersionAttr.ValueOrExpression);

        var deploymentEnvAttr = resourceOptions.AttributeOptions.First(a => a.Key == "deployment.environment");
        Assert.Equal("staging", deploymentEnvAttr.ValueOrExpression);

        var k8sClusterAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.cluster.name");
        Assert.Equal("staging-cluster", k8sClusterAttr.ValueOrExpression);

        var k8sNamespaceAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.namespace.name");
        Assert.Equal("microservices", k8sNamespaceAttr.ValueOrExpression);

        var k8sPodAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.pod.name");
        Assert.Equal("microservice-pod-abc123", k8sPodAttr.ValueOrExpression);

        var k8sDeploymentAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.deployment.name");
        Assert.Equal("microservice-deployment", k8sDeploymentAttr.ValueOrExpression);

        var k8sReplicaSetAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.replicaset.name");
        Assert.Equal("microservice-rs-xyz789", k8sReplicaSetAttr.ValueOrExpression);

        var k8sNodeAttr = resourceOptions.AttributeOptions.First(a => a.Key == "k8s.node.name");
        Assert.Equal("worker-node-1", k8sNodeAttr.ValueOrExpression);
    }
}
