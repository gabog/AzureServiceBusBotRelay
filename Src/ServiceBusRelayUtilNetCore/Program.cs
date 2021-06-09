using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(cb => cb.AddUserSecrets(typeof(Program).Assembly))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<DispatcherService>();
                });
    }
}