using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;

namespace GaboG.ServiceBusRelayUtilNetCore
{
    public class DispatcherService
    {
        private readonly string _connectionName;
        private readonly string _key;
        private readonly string _keyName;
        private readonly string _relayNamespace;
        private readonly string _targetServiceAddress;

        public DispatcherService(string relayNamespace, string connectionName, string keyName, string key, string targetServiceAddress)
        {
            _relayNamespace = relayNamespace;
            _connectionName = connectionName;
            _keyName = keyName;
            _key = key;
            _targetServiceAddress = targetServiceAddress;
        }

        public async Task RunAsync()
        {
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(_keyName, _key);
            var listener = new HybridConnectionListener(new Uri(string.Format("sb://{0}/{1}", _relayNamespace, _connectionName)), tokenProvider);
            
            // Subscribe to the status events.
            listener.Connecting += Listener_Connecting;
            listener.Offline += Listener_Offline;
            listener.Online += Listener_Online;

            // Provide an HTTP request handler
            listener.RequestHandler = ListenerRequestHandler;

            // Opening the listener establishes the control channel to
            // the Azure Relay service. The control channel is continuously 
            // maintained, and is reestablished when connectivity is disrupted.
            await listener.OpenAsync();
            Console.WriteLine("Server listening");

            // Start a new thread that will continuously read the console.
            await Console.In.ReadLineAsync();

            // Close the listener after you exit the processing loop.
            await listener.CloseAsync();
        }

        private void ListenerRequestHandler(RelayedHttpListenerContext context)
        {
            // Do something with context.Request.Url, HttpMethod, Headers, InputStream...
            context.Response.StatusCode = HttpStatusCode.OK;
            context.Response.StatusDescription = "OK";
            using (var sw = new StreamWriter(context.Response.OutputStream))
            {
                sw.WriteLine("hello!");
            }

            // The context MUST be closed here
            context.Response.Close();
        }

        private void Listener_Online(object sender, EventArgs e)
        {
            Console.WriteLine("Online");
        }

        private void Listener_Offline(object sender, EventArgs e)
        {
            Console.WriteLine("Offline");
        }

        private void Listener_Connecting(object sender, EventArgs e)
        {
            Console.WriteLine("Connecting");
        }
    }
}