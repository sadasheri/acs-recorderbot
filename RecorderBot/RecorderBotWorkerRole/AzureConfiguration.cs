// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureConfiguration.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// </copyright>
// <summary>
//   The configuration for azure.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sample.RecorderBot.WorkerRole
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Microsoft.Azure;
    using Microsoft.Skype.Bots.Media;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Sample.Common.Logging;
    using Sample.RecorderBot.FrontEnd;
    using Sample.RecorderBot.FrontEnd.Http;

    /// <summary>
    /// Reads the Configuration from service Configuration.
    /// </summary>
    internal class AzureConfiguration : IConfiguration
    {
        /// <summary>
        /// DomainNameLabel in NetworkConfiguration in .cscfg  <PublicIP name="instancePublicIP" domainNameLabel="pip"/>
        /// If the below changes, please change in the cscfg as well.
        /// </summary>
        public const string DomainNameLabel = "pip";

        /// <summary>
        /// The default endpoint key.
        /// </summary>
        private const string DefaultEndpointKey = "DefaultEndpoint";

        /// <summary>
        /// The instance call control endpoint key.
        /// </summary>
        private const string InstanceCallControlEndpointKey = "InstanceCallControlEndpoint";

        /// <summary>
        /// The instance media control endpoint key.
        /// </summary>
        private const string InstanceMediaControlEndpointKey = "InstanceMediaControlEndpoint";

        #region Service configuration keys

        /// <summary>
        /// The Ngrok TCP url key.
        /// </summary>
        private const string NgrokTcpUrlKey = "NgrokTcpUrl";

        /// <summary>
        /// Ngrok web fqdn key.
        /// </summary>
        private const string NgrokWebFqdnKey = "NgrokWebFqdn";

        /// <summary>
        /// Ngrok TCP url mapper key.
        /// </summary>
        private const string NgrokTcpCertFqdnKey = "NgrokTcpCertFqdn";

        /// <summary>
        /// The media local port key.
        /// </summary>
        private const string MediaLocalPortKey = "MediaLocalPort";

        /// <summary>
        /// The Azure dns name key.
        /// </summary>
        private const string AzureDnsNameKey = "AzureDnsName";

        /// <summary>
        /// The Azure certificate fqdn key.
        /// </summary>
        private const string AzureCertFqdnKey = "AzureCertFqdn";

        /// <summary>
        /// The default certificate key.
        /// </summary>
        private const string DefaultCertificateKey = "DefaultCertificate";

#endregion

        #region App Configuration Keys

        /// <summary>
        /// The application name key.
        /// </summary>
        private const string ApplicationNameKey = "ApplicationName";

        /// <summary>
        /// The application identifier key.
        /// </summary>
        private const string ApplicationIdentifierKey = "ApplicationIdentifier";

        /// <summary>
        /// The ACS application id key.
        /// </summary>
        private const string ACSApplicationInstanceIdKey = "ACSApplicationInstanceId";

        /// <summary>
        /// The ACS connection string key.
        /// </summary>
        private const string ACSConnectionStringKey = "ACSConnectionString";

        /// <summary>
        /// The place call endpoint URL key.
        /// </summary>
        private const string PlaceCallEndpointUrlKey = "PlaceCallEndpointUrl";

        #endregion

        /// <summary>
        /// The instance id token.
        /// Prefix of the InstanceId from the RoleEnvironment.
        /// </summary>
        private const string InstanceIdToken = "in_";

        /// <summary>
        /// localPort specified in <InputEndpoint name="DefaultCallControlEndpoint" protocol="tcp" port="443" localPort="9441" />
        /// in .csdef. This is needed for running in emulator. Currently only messaging can be debugged in the emulator.
        /// Media debugging in emulator will be supported in future releases.
        /// </summary>
        private const int DefaultPort = 9442;

        /// <summary>
        /// Graph logger.
        /// </summary>
        private Logger logger;

        /// <inheritdoc/>
        public IList<Uri> CallControlListeningUrls { get; private set; }

        /// <inheritdoc/>
        public Uri CallControlBaseUrl { get; private set; }

        /// <inheritdoc/>
        public Uri PlaceCallEndpointUrl { get; private set; }

        /// <inheritdoc/>
        public MediaPlatformSettings MediaPlatformSettings { get; private set; }

        /// <inheritdoc/>
        public string ApplicationName { get; private set; }

        /// <inheritdoc/>
        public string ApplicationIdentifier { get; private set; }

        /// <inheritdoc/>
        public string ACSApplicationInstanceId { get; private set; }

        /// <inheritdoc/>
        public string ACSConnectionString { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureConfiguration"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public AzureConfiguration(Logger logger)
        {
            this.logger = logger;
            this.InitializeAppConfiguration();
            this.TraceEndpointInfo();
            if (RoleEnvironment.IsEmulated)
            {
                InitializeLocalSettings();
            }
            else
            {
                InitializeCloudSettings();
            }

            this.TraceConfigValue("CallControlCallbackUri", this.CallControlBaseUrl);

            foreach (Uri uri in this.CallControlListeningUrls)
            {
                this.TraceConfigValue("Call control listening Uri", uri);
                this.logger.Info($"Call control listening Uri {uri}");
            }
        }

        /// <summary>
        /// Initialize from App.config
        /// </summary>
        public void InitializeAppConfiguration()
        {
            var placeCallEndpointUrlStr = this.GetString(PlaceCallEndpointUrlKey, true);
            if (!string.IsNullOrEmpty(placeCallEndpointUrlStr))
            {
                this.PlaceCallEndpointUrl = new Uri(placeCallEndpointUrlStr);
            }

            this.ApplicationName = ConfigurationManager.AppSettings[ApplicationNameKey];
            if (string.IsNullOrEmpty(this.ApplicationName))
            {
                throw new ConfigurationException("ApplicationName", "Update app.config in WorkerRole with ApplicationName");
            }

            this.ApplicationIdentifier = ConfigurationManager.AppSettings[ApplicationIdentifierKey];
            if (string.IsNullOrEmpty(this.ApplicationIdentifier))
            {
                throw new ConfigurationException("ApplicationIdentifier", "Update app.config in WorkerRole with ApplicationIdentifier");
            }

            this.ACSApplicationInstanceId = ConfigurationManager.AppSettings[ACSApplicationInstanceIdKey];
            if (string.IsNullOrEmpty(this.ACSApplicationInstanceId))
            {
                throw new ConfigurationException("ACSApplicationInstanceId", "Update app.config in WorkerRole with ACSApplicationInstanceId");
            }

            this.ACSConnectionString = ConfigurationManager.AppSettings[ACSConnectionStringKey];
            if (string.IsNullOrEmpty(this.ACSConnectionString))
            {
                throw new ConfigurationException("ACSConnectionString", "Update app.config in WorkerRole with ACSConnectionString from the azure communication service resource");
            }
        }

        /// <summary>
        /// Initialize from Serviceconfig.local
        /// </summary>
        public void InitializeLocalSettings()
        {
            var ngrokMediaUrl = this.GetString(NgrokTcpUrlKey);
            var ngrokSignallingUrl = this.GetString(NgrokWebFqdnKey, true);
            var ngrokMediaCertFqdn = this.GetString(NgrokTcpCertFqdnKey, true);

            RoleInstanceEndpoint defaultEndpoint = this.GetEndpoint(DefaultEndpointKey);

            var mediaPublicUrl = new Uri(ngrokMediaUrl);
            int mediaInstanceInternalPort = this.GetInt(MediaLocalPortKey, true);
            int mediaInstancePublicPort = mediaPublicUrl.Port;
            var mediaInstancePublicIpAddress = Dns.GetHostEntry(mediaPublicUrl.Host).AddressList[0];

            this.CallControlBaseUrl = new Uri(string.Format("https://{0}/{1}", ngrokSignallingUrl, HttpRouteConstants.OnIncomingRequestRoute));
            CallControlListeningUrls = new List<Uri>();
            CallControlListeningUrls.Add(new Uri("http://" + defaultEndpoint.IPEndpoint.Address + ":" + DefaultPort + "/"));

            X509Certificate2 defaultCertificate = this.GetCertificateFromStore(DefaultCertificateKey);

            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = mediaInstanceInternalPort,
                    InstancePublicIPAddress = mediaInstancePublicIpAddress,
                    InstancePublicPort = mediaInstancePublicPort,
                    ServiceFqdn = ngrokMediaCertFqdn,
                },

                ApplicationId = this.ApplicationIdentifier,
            };
        }

        /// <summary>
        /// Initialize from Serviceconfig.cloud
        /// </summary>
        public void InitializeCloudSettings()
        {
            var azureDnsName = this.GetString(AzureDnsNameKey);
            var azureCertFqdn = this.GetString(AzureCertFqdnKey, true);
            if (string.IsNullOrEmpty(azureCertFqdn))
            {
                azureCertFqdn = azureDnsName;
            }

            RoleInstanceEndpoint instanceCallControlEndpoint = this.GetEndpoint(InstanceCallControlEndpointKey);
            RoleInstanceEndpoint defaultEndpoint = this.GetEndpoint(DefaultEndpointKey);
            RoleInstanceEndpoint mediaControlEndpoint = this.GetEndpoint(InstanceMediaControlEndpointKey);

            //Instance internal ip address and port
            var instanceCallControlInternalIpAddress = instanceCallControlEndpoint.IPEndpoint.Address.ToString();
            var instanceCallControlInternalPort = instanceCallControlEndpoint.IPEndpoint.Port;

            //Instance public ip address and port
            var instanceCallControlPublicIpAddress = this.GetInstancePublicIpAddress(azureDnsName);
            var instanceCallControlPublicPort = instanceCallControlEndpoint.PublicIPEndpoint.Port;

            //Media public ip address and port
            var mediaInstanceInternalPort = mediaControlEndpoint.IPEndpoint.Port;
            var mediaInstancePublicPort = mediaControlEndpoint.PublicIPEndpoint.Port;

            string instanceCallControlIpEndpoint = string.Format("{0}:{1}", instanceCallControlInternalIpAddress, instanceCallControlInternalPort);

            this.CallControlBaseUrl = new Uri(string.Format("https://{0}:{1}/{2}",
                                            azureCertFqdn,
                                            instanceCallControlPublicPort,
                                            HttpRouteConstants.OnIncomingRequestRoute));

            CallControlListeningUrls = new List<Uri>();
            this.CallControlListeningUrls.Add(new Uri("https://" + instanceCallControlIpEndpoint + "/"));
            this.CallControlListeningUrls.Add(new Uri("https://" + defaultEndpoint.IPEndpoint + "/"));

            X509Certificate2 defaultCertificate = this.GetCertificateFromStore(DefaultCertificateKey);

            this.MediaPlatformSettings = new MediaPlatformSettings()
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings()
                {
                    CertificateThumbprint = defaultCertificate.Thumbprint,
                    InstanceInternalPort = mediaInstanceInternalPort,
                    InstancePublicIPAddress = instanceCallControlPublicIpAddress,
                    InstancePublicPort = mediaInstancePublicPort,
                    ServiceFqdn = azureCertFqdn,
                },

                ApplicationId = this.ApplicationIdentifier,
            };
        }

        /// <summary>
        /// Dispose the Configuration.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Write endpoint info into the debug logs.
        /// </summary>
        private void TraceEndpointInfo()
        {
            string[] endpoints = RoleEnvironment.IsEmulated
                ? new string[] { DefaultEndpointKey }
                : new string[] { DefaultEndpointKey, InstanceMediaControlEndpointKey };

            foreach (string endpointName in endpoints)
            {
                RoleInstanceEndpoint endpoint = this.GetEndpoint(endpointName);
                StringBuilder info = new StringBuilder();
                info.AppendFormat("Internal=https://{0}, ", endpoint.IPEndpoint);
                string publicInfo = endpoint.PublicIPEndpoint == null ? "-" : endpoint.PublicIPEndpoint.Port.ToString();
                info.AppendFormat("PublicPort={0}", publicInfo);
                this.TraceConfigValue(endpointName, info);
            }
        }

        /// <summary>
        /// Write debug entries for the configuration.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="value">Configuration value.</param>
        private void TraceConfigValue(string key, object value)
        {
            this.logger.Info($"{key} ->{value}");
        }

        /// <summary>
        /// Lookup endpoint by its name.
        /// </summary>
        /// <param name="name">Endpoint name.</param>
        /// <returns>Role instance endpoint.</returns>
        private RoleInstanceEndpoint GetEndpoint(string name)
        {
            if (!RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.TryGetValue(name, out RoleInstanceEndpoint endpoint))
            {
                throw new ConfigurationException(name, $"No endpoint with name '{name}' was found.");
            }

            return endpoint;
        }

        /// <summary>
        /// Lookup configuration value.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="allowEmpty">If empty configurations are allowed.</param>
        /// <returns>Configuration value, if found.</returns>
        private string GetString(string key, bool allowEmpty = false)
        {
            string s = CloudConfigurationManager.GetSetting(key);

            this.TraceConfigValue(key, s);

            if (!allowEmpty && string.IsNullOrWhiteSpace(s))
            {
                throw new ConfigurationException(key, "The Configuration value is null or empty.");
            }

            return s;
        }

        /// <summary>
        /// Lookup configuration value.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <param name="allowEmpty">If empty configurations are allowed.</param>
        /// <returns>Configuration value, if found.</returns>
        private int GetInt(string key, bool allowEmpty = false)
        {
            string s = CloudConfigurationManager.GetSetting(key);

            this.TraceConfigValue(key, s);

            if (!int.TryParse(s, out int value))
            {
                if (allowEmpty)
                {
                    return 0;
                }

                throw new ConfigurationException(key, "The Configuration value is null or empty.");
            }

            return value;
        }

        /// <summary>
        /// Retrieve configuration, stored as comma separated, as an array.
        /// </summary>
        /// <param name="key">Configuration key containing the setting.</param>
        /// <returns>Configuration value split into an array.</returns>
        private List<string> GetStringList(string key)
        {
            return this.GetString(key).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        /// <summary>
        /// Helper to search the certificate store by its thumbprint.
        /// </summary>
        /// <param name="key">Configuration key containing the Thumbprint to search.</param>
        /// <returns>Certificate if found.</returns>
        private X509Certificate2 GetCertificateFromStore(string key)
        {
            string thumbprint = this.GetString(key);

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                X509Certificate2Collection certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                if (certs.Count != 1)
                {
                    throw new ConfigurationException(key, $"No certificate with thumbprint {thumbprint} was found in the machine store.");
                }

                return certs[0];
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Get the PIP for this instance.
        /// </summary>
        /// <param name="publicFqdn">DNS name for this service.</param>
        /// <returns>IPAddress.</returns>
        private IPAddress GetInstancePublicIpAddress(string publicFqdn)
        {
            // get the instanceId for the current instance. It will be of the form  XXMediaBotRole_IN_0. Look for IN_ and then extract the number after it
            // Assumption: in_<instanceNumber> will the be the last in the instanceId
            string instanceId = RoleEnvironment.CurrentRoleInstance.Id;
            int instanceIdIndex = instanceId.IndexOf(InstanceIdToken, StringComparison.OrdinalIgnoreCase);
            if (!int.TryParse(instanceId.Substring(instanceIdIndex + InstanceIdToken.Length), out int instanceNumber))
            {
                var err = $"Couldn't extract Instance index from {instanceId}";
                this.logger.Error(err);
                throw new Exception(err);
            }

            // for example: instance0 for fooservice.cloudapp.net will have hostname as pip.0.fooservice.cloudapp.net
            string instanceHostName = DomainNameLabel + "." + instanceNumber + "." + publicFqdn;
            IPAddress[] instanceAddresses = Dns.GetHostEntry(instanceHostName).AddressList;
            if (instanceAddresses.Length == 0)
            {
                throw new InvalidOperationException("Could not resolve the PIP hostname. Please make sure that PIP is properly configured for the service");
            }

            return instanceAddresses[0];
        }
    }
}
