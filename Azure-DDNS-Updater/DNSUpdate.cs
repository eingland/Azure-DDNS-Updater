using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDDNSUpdater
{
    class DNSUpdate : IHostedService, IDisposable
    {
        private IConfiguration _config;
        private Task _executingTask;
        private readonly ILogger _logger;
        private TelemetryClient _telemetryClient;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private readonly string clientId;
        private string recordSetName;
        private string resourceGroupName;
        private string secret;
        private string subscriptionId;
        private string tenantId;
        private string zoneName;
        private int refreshInterval;

        public DNSUpdate(ILogger<DNSUpdate> logger, TelemetryClient tc, IConfiguration config)
        {
            _logger = logger;
            this._telemetryClient = tc;
            _config = config;

            tenantId = _config["tenantId"];
            clientId = _config["clientId"];
            secret = _config["secret"];
            subscriptionId = _config["subscriptionId"];
            resourceGroupName = _config["resourceGroupName"];
            zoneName = _config["zoneName"];
            recordSetName = _config["recordSetName"];
            refreshInterval = int.Parse(_config["refreshInterval"], CultureInfo.InvariantCulture);
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This will cause the loop to stop if the service is stopped
            while (!stoppingToken.IsCancellationRequested)
            {
                await Update().ConfigureAwait(true);
                // Wait 5 minutes before running again.
                await Task.Delay(TimeSpan.FromMinutes(refreshInterval), stoppingToken).ConfigureAwait(true);
            }
        }

        public void Dispose()
        {
            _stoppingCts.Cancel();
            _stoppingCts.Dispose();
        }

        public async Task<string> GetPublicIP()
        {
            try
            {
                var httpClient = new HttpClient();
                var ip = await httpClient.GetStringAsync(new Uri("https://api.ipify.org")).ConfigureAwait(true);
                httpClient.Dispose();
                return ip;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                _telemetryClient.TrackException(e);
                return null;
            }

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Store the task we're executing
            _executingTask = ExecuteAsync(_stoppingCts.Token);

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite,
                    cancellationToken)).ConfigureAwait(true);
            }
        }

        public async Task Update()
        {

            if (tenantId == null || clientId == null || secret == null || subscriptionId == null || resourceGroupName == null || zoneName == null || recordSetName == null)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                _logger.LogError("Configuration settings not set.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
            else
            {
                try
                {
                    using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
                    {
                        var ipAddress = await GetPublicIP().ConfigureAwait(true);

                        if (null != ipAddress)
                        {
                            // Build the service credentials and DNS management client
                            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret).ConfigureAwait(true);
                            var dnsClient = new DnsManagementClient(serviceCreds);
                            dnsClient.SubscriptionId = subscriptionId;

                            var currentRecordSet = await dnsClient.RecordSets.GetAsync(resourceGroupName, zoneName, recordSetName, RecordType.A).ConfigureAwait(true);

                            if (currentRecordSet.ARecords.Where(r => r.Ipv4Address == ipAddress).Count() == 1)
                            {
                                _logger.LogInformation("Zone already up to date with IP: " + ipAddress);
                                _telemetryClient.TrackEvent("IP Address already up to date");
                            }
                            else
                            {
                                // Create record set parameters
                                var recordSetParams = new RecordSet();
                                recordSetParams.TTL = 300;

                                // Add records to the record set parameter object.
                                recordSetParams.ARecords = new List<ARecord>();
                                recordSetParams.ARecords.Add(new ARecord(ipAddress));

                                // Create the actual record set in Azure DNS
                                // Note: no ETAG checks specified, will overwrite existing record set if one exists
                                await dnsClient.RecordSets.CreateOrUpdateAsync(resourceGroupName, zoneName, recordSetName, RecordType.A, recordSetParams).ConfigureAwait(true);

                                _logger.LogInformation("Zone Updated with IP: " + ipAddress);
                                _telemetryClient.TrackEvent("Updated IP Address");
                            }

                            dnsClient.Dispose();
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                    _telemetryClient.TrackException(e);
                }
            }

        }
    }
}
