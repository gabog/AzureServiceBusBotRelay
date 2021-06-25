using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBusBotRelay.Core
{
    /// <summary>
    /// Main Console App class
    /// </summary>
    public static class Program
    {
        public static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            var relayNamespace = $"{Configuration["Relay:Namespace"]}.servicebus.windows.net";
            var connectionName = Configuration["Relay:ConnectionName"];
            var keyName = Configuration["Relay:PolicyName"];
            var key = Configuration["Relay:PolicyKey"];
            var targetServiceAddress = new Uri(Configuration["Relay:TargetServiceAddress"]);
            var httpMode = Configuration["Relay:Mode"].Equals("http", StringComparison.CurrentCultureIgnoreCase);

            if (httpMode)
            {
                // Create the Http hybrid proxy listener
                var hybridProxy = new HttpListener(relayNamespace, connectionName, keyName, key, targetServiceAddress, new CancellationTokenSource());

                // Opening the listener establishes the control channel to
                // the Azure Relay service. The control channel is continuously
                // maintained, and is reestablished when connectivity is disrupted.
                await hybridProxy.OpenAsync();
                Console.WriteLine("Server listening");

                // Start a new thread that will continuously read the console.
                await hybridProxy.ListenAsync();

                // Close the listener after you exit the processing loop.
                await hybridProxy.CloseAsync();
            }
            else // WebSockets Relay Mode
            {
                // Create the WebSockets hybrid proxy listener
                var hybridProxy = new WebSocketListener(relayNamespace, connectionName, keyName, key, targetServiceAddress, new CancellationTokenSource());

                // Opening the listener establishes the control channel to
                // the Azure Relay service. The control channel is continuously
                // maintained, and is reestablished when connectivity is disrupted.
                await hybridProxy.OpenAsync();
                Console.WriteLine("Server listening");

                // Start a new thread that will continuously read the console.
                await hybridProxy.ListenAsync();

                // Close the listener after you exit the processing loop.
                await hybridProxy.CloseAsync();
            }

        }
    }
}
