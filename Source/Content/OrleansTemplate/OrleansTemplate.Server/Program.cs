namespace OrleansTemplate.Server
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Orleans;
    using Orleans.Configuration;
    using Orleans.Hosting;
    using Orleans.Statistics;
    using OrleansTemplate.Abstractions.Constants;
    using OrleansTemplate.Grains;
    using OrleansTemplate.Server.Options;

    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                var siloHost = CreateSiloHostBuilder(args).Build();
                await siloHost.StartAsync();

                Console.Read();

                await siloHost.StopAsync();
                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return -1;
            }
        }

        private static ISiloHostBuilder CreateSiloHostBuilder(string[] args)
        {
            StorageOptions storageOptions = null;
            return new SiloHostBuilder()
                .ConfigureAppConfiguration(
                    (context, configurationBuilder) =>
                    {
                        context.HostingEnvironment.EnvironmentName = GetEnvironmentName();
                        AddConfiguration(configurationBuilder, context.HostingEnvironment.EnvironmentName, args);
                    })
                .ConfigureServices(
                    (context, services) =>
                    {
                        services.Configure<ApplicationOptions>(context.Configuration);
                        services.Configure<ClusterOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.Cluster)));
                        services.Configure<StorageOptions>(context.Configuration.GetSection(nameof(ApplicationOptions.Storage)));

                        storageOptions = services.BuildServiceProvider().GetRequiredService<IOptions<StorageOptions>>().Value;
                    })
                .UseAzureStorageClustering(options => options.ConnectionString = storageOptions.ConnectionString)
                .ConfigureEndpoints(
                    EndpointOptions.DEFAULT_SILO_PORT,
                    EndpointOptions.DEFAULT_GATEWAY_PORT,
                    listenOnAnyHostAddress: true) // TODO: Figure out how to set this to false when hostingEnvironment.IsDevelopment()
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(HelloGrain).Assembly).WithReferences())
#if (ApplicationInsights)
                .AddApplicationInsightsTelemetryConsumer("")
#endif
                .ConfigureLogging(logging => logging.AddConsole())
                .AddAzureTableGrainStorageAsDefault(
                    options =>
                    {
                        options.ConnectionString = storageOptions.ConnectionString;
                        options.UseJson = true;
                    })
                .UseAzureTableReminderService(options => options.ConnectionString = storageOptions.ConnectionString)
                .UseTransactions(withStatisticsReporter: true)
                .AddAzureTableTransactionalStateStorageAsDefault(options => options.ConnectionString = storageOptions.ConnectionString)
                .AddSimpleMessageStreamProvider(StreamProviderName.Default)
                .AddAzureTableGrainStorage("PubSubStore", options => options.ConnectionString = storageOptions.ConnectionString)
                .UseIf(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                    x => x.UsePerfCounterEnvironmentStatistics())
                .UseDashboard();
        }

        private static IConfigurationBuilder AddConfiguration(
            IConfigurationBuilder configurationBuilder,
            string environmentName,
            string[] args) =>
            configurationBuilder
                // Add configuration from the appsettings.json file.
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                // Add configuration from an optional appsettings.development.json, appsettings.staging.json or
                // appsettings.production.json file, depending on the environment. These settings override the ones in
                // the appsettings.json file.
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                // This reads the configuration keys from the secret store. This allows you to store connection strings
                // and other sensitive settings, so you don't have to check them into your source control provider.
                // Only use this in Development, it is not intended for Production use. See
                // http://docs.asp.net/en/latest/security/app-secrets.html
                .AddIf(
                    string.Equals(environmentName, EnvironmentName.Development, StringComparison.Ordinal),
                    x => x.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true))
                // Add configuration specific to the Development, Staging or Production environments. This config can
                // be stored on the machine being deployed to or if you are using Azure, in the cloud. These settings
                // override the ones in all of the above config files. See
                // http://docs.asp.net/en/latest/security/app-secrets.html
                .AddEnvironmentVariables()
                // Add command line options. These take the highest priority.
                .AddIf(
                    args != null,
                    x => x.AddCommandLine(args));

        private static string GetEnvironmentName() =>
            Environment.GetEnvironmentVariable("ENVIRONMENT") ?? EnvironmentName.Production;
    }
}
