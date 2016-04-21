namespace DevOps.LoadTest.AutoScaling
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Management.Monitoring.Autoscale;
    using Microsoft.WindowsAzure.Management.Monitoring.Autoscale.Models;
    using Microsoft.WindowsAzure.Management.Monitoring.Metrics;
    using Microsoft.WindowsAzure.Management.Monitoring.Metrics.Models;
    using Microsoft.WindowsAzure.Management.Monitoring.Utilities;

    public class CloudServiceAutoscalerHelper
    {
        private readonly AutoscaleClient autoscaleClient;
        private readonly MetricsClient metricsClient;
        private readonly string cloudServiceName;
        private readonly string roleName;
        private readonly string deploymentName;
        private readonly bool isProduction;

        public CloudServiceAutoscalerHelper()
        {
            // Read configuration settings.
            var subscriptionId = ConfigurationManager.AppSettings["SubscriptionId"];
            var certThumbprint = ConfigurationManager.AppSettings["CertThumbprint"];
            this.cloudServiceName = ConfigurationManager.AppSettings["CloudServiceName"];
            this.roleName = ConfigurationManager.AppSettings["RoleName"];
            this.deploymentName = ConfigurationManager.AppSettings["DeploymentName"];
            this.isProduction = bool.Parse(ConfigurationManager.AppSettings["IsProduction"]);

            // Get the certificate from the local store.
            var cert = CertificateHelper.GetCertificate(StoreName.My, StoreLocation.CurrentUser, certThumbprint);

            // Create the autoscale client.
            this.autoscaleClient = new AutoscaleClient(new CertificateCloudCredentials(subscriptionId, cert));

            // Create the metrics client.
            this.metricsClient = new MetricsClient(new CertificateCloudCredentials(subscriptionId, cert));
        }

        public AutoscaleSettingGetResponse GetSettings()
        {
            var resourceId = AutoscaleResourceIdBuilder.BuildCloudServiceResourceId(cloudServiceName, roleName, isProduction);
            return autoscaleClient.Settings.Get(resourceId);
        }

        public MetricDefinitionListResponse GetMetrics()
        {
            var resourceId = ResourceIdBuilder.BuildCloudServiceResourceId(cloudServiceName, deploymentName);
            return metricsClient.MetricDefinitions.List(resourceId, null, string.Empty);
        }

        public MetricValueListResponse GetMetricValues(string metricName, string metricNamespace)
        {
            var resourceId = ResourceIdBuilder.BuildCloudServiceResourceId(cloudServiceName, deploymentName);
            return metricsClient.MetricValues.List(resourceId, new List<string> { metricName }, metricNamespace, TimeSpan.FromMinutes(5), DateTime.UtcNow.AddMinutes(-15), DateTime.UtcNow.AddMinutes(-5));
        }

        public AzureOperationResponse CreateOrUpdateSettings(AutoscaleSettingCreateOrUpdateParameters settings)
        {
            var resourceId = AutoscaleResourceIdBuilder.BuildCloudServiceResourceId(cloudServiceName, roleName, isProduction);
            return autoscaleClient.Settings.CreateOrUpdate(resourceId, settings);
        }

        public AzureOperationResponse DeleteSettings()
        {
            var resourceId = AutoscaleResourceIdBuilder.BuildCloudServiceResourceId(cloudServiceName, roleName, isProduction);
            return autoscaleClient.Settings.Delete(resourceId);
        }

        public AutoscaleSettingCreateOrUpdateParameters CreateCpuSettings(int minInstances, int maxInstances)
        {
            return new AutoscaleSettingCreateOrUpdateParameters
            {
                Setting = new AutoscaleSetting
                {
                    Enabled = true,
                    Profiles = new List<AutoscaleProfile>
                    {
                        new AutoscaleProfile()
                        {
                            Name = "Scale.Cpu",
                            Capacity = new ScaleCapacity
                            {
                                Default = minInstances.ToString(CultureInfo.InvariantCulture),
                                Minimum = minInstances.ToString(CultureInfo.InvariantCulture),
                                Maximum = maxInstances.ToString(CultureInfo.InvariantCulture)
                            },
                            Rules = new List<ScaleRule>
                                    {
                                        this.CreateCpuRule(true),
                                        this.CreateCpuRule(false)
                                    }
                        }
                    }
                }
            };
        }

        private ScaleRule CreateCpuRule(bool increase)
        {
            return new ScaleRule()
            {
                // Define the MetricTrigger Properties
                MetricTrigger = new MetricTrigger()
                {
                    MetricName = "Percentage CPU",
                    MetricNamespace = "",
                    MetricSource = AutoscaleMetricSourceBuilder.BuildCloudServiceMetricSource(cloudServiceName, roleName, isProduction),
                    TimeGrain = TimeSpan.FromMinutes(5), // recomended 5 min, at least 1 min
                    TimeWindow = TimeSpan.FromMinutes(25), //recomended 25 min, at least 5 min
                    TimeAggregation = TimeAggregationType.Average,
                    Statistic = MetricStatisticType.Average,
                    Operator = increase ? ComparisonOperationType.GreaterThanOrEqual : ComparisonOperationType.LessThanOrEqual,
                    Threshold = increase ? 40.0 : 20.0
                },
                // Define the ScaleAction Properties
                ScaleAction = new ScaleAction()
                {
                    Direction = increase ? ScaleDirection.Increase : ScaleDirection.Decrease,
                    Type = ScaleType.ChangeCount,
                    Value = "1",
                    Cooldown = TimeSpan.FromMinutes(10) // at least 5 min
                }
            };
        }
    }
}
