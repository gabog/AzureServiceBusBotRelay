using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
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
        public class RelayOptions
        {
            [Option(
                'n',
                "namespace",
                Required = true,
                HelpText = "The name of the relay's namespace, e.g. '[Your Namespace].servicebus.windows.net'")]
            public string RelayNamespace { get; set; }
            
            [Option(
                'r', 
                "relay",
                Required = true,
                HelpText = "The name of the relay")]
            public string RelayName { get; set; }
            
            [Option(
                'p', 
                "policy",
                Required = true,
                HelpText = "The name of the relay's Shared Access Policy")]
            public string PolicyName { get; set; }
            
            [Option(
                'k', 
                "key",
                Required = true,
                HelpText = "The Shared Access Policy's key")]
            public string PolicyKey { get; set; }
            
            [Option(
                'b', 
                "botUri",
                Required = true,
                HelpText = "The url to your local bot e.g. 'http://localhost:[PORT]'")]
            public string TargetServiceAddress { get; set; }
        }

        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<RelayOptions>(args)
                .WithParsed(opt => CreateHostBuilder(opt).Build().Run());
        }

        public static IHostBuilder CreateHostBuilder(RelayOptions options) =>
            Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(cb => cb.AddUserSecrets(typeof(Program).Assembly))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<RelayOptions>(options);
                    services.AddHostedService<DispatcherService>();
                });
    }
}