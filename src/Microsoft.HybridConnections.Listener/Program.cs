using Microsoft.Azure.Relay;
using Microsoft.Extensions.Configuration;
using Microsoft.HybridConnections.Core;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.HybridConnections.Listener
{
    /// <summary>
    /// Main Console App class
    /// </summary>
    public static class Program
    {
        public static IConfiguration Configuration { get; set; }
        public static HttpListener HttpRelayListener { get; set; }

        public static WebSocketListener WebSocketListener { get; set; }

        private static string TargetHttpRelay;
        private static bool IsVerboseLogs;

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task<bool> RunAsync()
        {
            try
            {
                var relayNamespace = $"{Configuration["Relay:Namespace"]}.servicebus.windows.net";
                var connectionName = Configuration["Relay:ConnectionName"];
                var keyName = Configuration["Relay:PolicyName"];
                var key = Configuration["Relay:PolicyKey"];
                var IsHttpRelayMode = Configuration["Relay:Mode"].Equals("http", StringComparison.CurrentCultureIgnoreCase);

                TargetHttpRelay = Configuration["Relay:TargetServiceAddress"];
                IsVerboseLogs = bool.Parse(Configuration["Relay:Log:Verbose"]);

                if (IsHttpRelayMode)
                {
                    // Create the Http hybrid proxy listener
                    HttpRelayListener = new HttpListener(relayNamespace, connectionName, keyName, key, TargetHttpRelay, new CancellationTokenSource());

                    // Opening the listener establishes the control channel to
                    // the Azure Relay service. The control channel is continuously
                    // maintained, and is reestablished when connectivity is disrupted.
                    await HttpRelayListener.OpenAsync(ProcessHttpMessagesHandler);
                    Console.WriteLine("Http Server listening");

                    // Start a new thread that will continuously read the console.
                    await HttpRelayListener.ListenAsync();

                    // Return true, if the cancellation was requested, otherwise - false
                    return HttpRelayListener.CTS.IsCancellationRequested;
                }
                else // WebSockets Relay Mode
                {
                    // Create the WebSockets hybrid proxy listener
                    var webSocketListener = new WebSocketListener(relayNamespace, connectionName, keyName, key, new CancellationTokenSource());

                    // Opening the listener establishes the control channel to
                    // the Azure Relay service. The control channel is continuously
                    // maintained, and is reestablished when connectivity is disrupted.
                    await webSocketListener.OpenAsync();
                    Console.WriteLine("WebSocket Server listening");

                    // Start a new thread that will continuously read the from the websocket and write to the target Http endpoint.
                    await webSocketListener.ListenAsync(ProcessWebsocketMessagesHandler);

                    // Close Websocket connection
                    await webSocketListener.CloseAsync();

                    Console.WriteLine("Awaiting for new received messages over websocket connection...");

                    // Return true, if the cancellation was requested, otherwise - false
                    return webSocketListener.CTS.IsCancellationRequested;
                }
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync(e.Message);
                return true;
            }
        }

        /// <summary>
        /// Listener Response Handler
        /// </summary>
        /// <param name="context"></param>
        static async void ProcessHttpMessagesHandler(RelayedHttpListenerContext context)
        {
            var startTimeUtc = DateTime.UtcNow;

            try
            {
                // Send the request message to the target listener
                Console.WriteLine("Sending the request message to {0}...", TargetHttpRelay);
                var responseMessage = await HttpRelayListener.SendHttpRequestMessageAsync(context);

                // Send the response message back to the caller
                Console.WriteLine("Sending the response message to the context owner...");
                await HttpRelayListener.SendResponseAsync(context, responseMessage);
            }

            catch (Exception ex)
            {
                Logger.LogException(ex);
                HttpRelayListener.SendErrorResponse(ex, context);
            }
            finally
            {
                Logger.LogRequest(startTimeUtc);
                // Confirm the response has been sent
                context.Response.StatusCode = System.Net.HttpStatusCode.OK;
                context.Response.StatusDescription = "OK";
                using (var sw = new StreamWriter(context.Response.OutputStream))
                {
                    sw.WriteLine("Response message has been sent");
                }

                // The context MUST be closed here
                await context.Response.CloseAsync();
            }
        }


        /// <summary>
        /// The method initiates the connection.
        /// </summary>
        /// <param name="relayConnection"></param>
        /// <param name="cts"></param>
        static async void ProcessWebsocketMessagesHandler(HybridConnectionStream relayConnection, CancellationTokenSource cts)
        {
            Console.WriteLine("New Websocket session");
            // The connection is a relay fork.
            // We put a stream reader on the input stream and a stream writer over to the target connection
            // that allows us to read UTF-8 text data that comes from
            // the sender and to write text to the target endpoint.
            var reader = new StreamReader(relayConnection);

            Console.WriteLine("Awaiting for the input messages...");
            // Read a line of input until the end of the buffer
            var data = await reader.ReadToEndAsync();

            Console.WriteLine($"Received data of the {data.Length} bytes length over websocket connection.");

            // Deserialize the websocket data into HttpRequestMessage
            var requestMessage = RelayedHttpListenerRequestSerializer.Deserialize(data);

            // Send the request message to the target listener
            Console.WriteLine("Sending the request message to {0}...", TargetHttpRelay);
            await SendHttpRequestAsync(requestMessage);

            // If there's no input data, signal that
            // you will no longer send data on this connection.
            await relayConnection.ShutdownAsync(cts.Token);

            Console.WriteLine("End web socket session");

            // closing the connection from this end
            await relayConnection.CloseAsync(cts.Token);
        }


        /// <summary>
        /// Creates and sends the Stream message over Http Relay connection
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        private static async Task SendHttpRequestAsync(HttpRequestMessage requestMessage)
        {
            try
            {
                // Send the request message via Http
                using (var httpClient = new HttpClient { BaseAddress = new Uri(TargetHttpRelay, UriKind.RelativeOrAbsolute) })
                {
                    httpClient.DefaultRequestHeaders.ExpectContinue = false;
                    await httpClient.SendAsync(requestMessage);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                throw;
            }

            if (IsVerboseLogs)
            {
                // Log the activity message
                await Logger.LogRequestActivityAsync(requestMessage);
            }
        }
    }
}
