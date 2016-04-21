namespace DevOps.LoadTest.AutoScaling
{
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Management.Monitoring.Autoscale;
    using Microsoft.WindowsAzure.Management.Monitoring.Autoscale.Models;
    using Microsoft.WindowsAzure.Management.Monitoring.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;

    public class WebsiteAutoscalerHelper
    {
        private readonly AutoscaleClient autoscaleClient;
        private readonly string webspaceName;
        private readonly string hostingPlanName;


        public WebsiteAutoscalerHelper()
        {
            // Read configuration settings.
            var subscriptionId = ConfigurationManager.AppSettings["SubscriptionId"];
            this.webspaceName = ConfigurationManager.AppSettings["WebspaceName"];
            this.hostingPlanName = ConfigurationManager.AppSettings["HostingPlanName"];

            // Get the certificate from appsettings.
            var cert = CertificateHelper.GetCertificateFromAppSettings("ManagementCertificate");

            // Create the autoscale client.
            this.autoscaleClient = new AutoscaleClient(new CertificateCloudCredentials(subscriptionId, cert));
        }

        public AutoscaleSettingGetResponse GetSettings()
        {
            var resourceId = AutoscaleResourceIdBuilder.BuildWebSiteResourceId(this.webspaceName, this.hostingPlanName);
            return autoscaleClient.Settings.Get(resourceId);
        }

        public AzureOperationResponse CreateOrUpdateSettings(AutoscaleSettingCreateOrUpdateParameters settings)
        {
            var resourceId = AutoscaleResourceIdBuilder.BuildWebSiteResourceId(this.webspaceName, this.hostingPlanName);
            return autoscaleClient.Settings.CreateOrUpdate(resourceId, settings);
        }

        public AzureOperationResponse DeleteSettings()
        {
            var resourceId = AutoscaleResourceIdBuilder.BuildWebSiteResourceId(this.webspaceName, this.hostingPlanName);
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
                    MetricSource = AutoscaleMetricSourceBuilder.BuildWebSiteMetricSource(this.webspaceName, this.hostingPlanName),
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
