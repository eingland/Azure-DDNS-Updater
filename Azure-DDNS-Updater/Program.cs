using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AzureDDNSUpdater
{
    class Program
    {
        static async Task Main()
        {
            var host = new HostBuilder()
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddJsonFile("appsettings.Development.json", optional: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                services.AddHostedService<DNSUpdate>();

                services.AddApplicationInsightsTelemetryWorkerService();
            })
            .UseConsoleLifetime()
            .Build();

            using (host)
            {
                // Start the host
                await host.StartAsync().ConfigureAwait(true);

                // Wait for the host to shutdown
                await host.WaitForShutdownAsync().ConfigureAwait(true);
            }
        }

    }
}
