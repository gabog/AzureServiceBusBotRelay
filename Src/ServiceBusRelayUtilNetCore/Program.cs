using System;
using System.IO;
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
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            var relayNamespace = Configuration["RelayNamespace"];
            var connectionName = Configuration["ConnectionName"];
            var keyName = Configuration["KeyName"];
            var key = Configuration["Key"];
            var targetServiceAddress = Configuration["TargetServiceAddress"];

            var service = new DispatcherService(relayNamespace, connectionName, keyName, key, targetServiceAddress);
            service.RunAsync().GetAwaiter().GetResult();
        }
    }
}