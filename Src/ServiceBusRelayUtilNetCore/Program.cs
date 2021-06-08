using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

// https://docs.microsoft.com/en-us/azure/service-bus-relay/service-bus-relay-rest-tutorial
// https://github.com/Azure/azure-relay-dotnet
// https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-hybrid-connections-http-requests-dotnet-get-started

// This is what I think I need
// https://github.com/Azure/azure-relay/blob/master/samples/hybrid-connections/dotnet/hcreverseproxy/README.md
// https://github.com/Azure/azure-relay/tree/master/samples/hybrid-connections/dotnet/hcreverseproxy

// Publish
// https://stackoverflow.com/questions/44074121/build-net-core-console-application-to-output-an-exe
// https://docs.microsoft.com/en-us/dotnet/core/rid-catalog

namespace GaboG.ServiceBusRelayUtilNetCore
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .AddUserSecrets(typeof(Program).Assembly);
            Configuration = builder.Build();

            RunAsync().GetAwaiter().GetResult();
        }

        static async Task RunAsync()
        {
            var relayNamespace = Configuration["RelayNamespace"];
            var connectionName = Configuration["RelayName"];
            var keyName = Configuration["PolicyName"];
            var key = Configuration["PolicyKey"];
            var targetServiceAddress = new Uri(Configuration["TargetServiceAddress"]);

            var hybridProxy = new DispatcherService(relayNamespace, connectionName, keyName, key, targetServiceAddress);

            await hybridProxy.OpenAsync(CancellationToken.None);

            Console.ReadLine();

            await hybridProxy.CloseAsync(CancellationToken.None);
        }
    }
}