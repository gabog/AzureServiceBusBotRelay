using Microsoft.Azure.Relay;
using Microsoft.ServiceBusBotRelay.Core.Extensions;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBusBotRelay.Core
{
    internal class HttpListener
    {
        private readonly HttpClient _httpClient;
        private readonly string _hybridConnectionSubPath;
        private readonly HybridConnectionListener _listener;
        private readonly Uri _targetServiceAddress;
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="relayNamespace"></param>
        /// <param name="connectionName"></param>
        /// <param name="keyName"></param>
        /// <param name="key"></param>
        /// <param name="targetServiceAddress"></param>
        public HttpListener(string relayNamespace, string connectionName, string keyName, string key, Uri targetServiceAddress, CancellationTokenSource cts)
        {
            _targetServiceAddress = targetServiceAddress;
            _cancellationTokenSource = cts;

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);
            _listener = new HybridConnectionListener(new Uri($"sb://{relayNamespace}/{connectionName}"), tokenProvider);

            // Subscribe to the status events.
            _listener.Connecting += (o, e) => { Console.WriteLine("Connecting"); };
            _listener.Offline += (o, e) => { Console.WriteLine("Offline"); };
            _listener.Online += (o, e) => { Console.WriteLine("Online"); };

            _httpClient = new HttpClient
            {
                BaseAddress = targetServiceAddress
            };
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;

            _hybridConnectionSubPath = _listener.Address.AbsolutePath.EnsureEndsWith("/");
        }

        /// <summary>
        // Opening the listener establishes the control channel to
        // the Azure Relay service. The control channel is continuously
        // maintained, and is reestablished when connectivity is disrupted.
        /// </summary>
        /// <returns></returns>
        public async Task OpenAsync()
        {
            _listener.RequestHandler = this.ListenerRequestHandler;
            await _listener.OpenAsync(_cancellationTokenSource.Token);
            Console.WriteLine($"Azure Service Bus is listening on \n\r\t{_listener.Address}\n\rand routing requests to \n\r\t{_httpClient.BaseAddress}\n\r\n\r");
            Console.WriteLine("Press [Enter] to exit");

            // Provide callback for a cancellation token that will close the listener.
            _cancellationTokenSource.Token.Register(() => _listener.CloseAsync(CancellationToken.None));
        }


        /// <summary>
        /// Starts listening to the messages
        /// </summary>
        /// <returns></returns>
        public async Task ListenAsync()
        {
            // Start a new thread that will continuously read the console.
            await Console.In.ReadLineAsync().ContinueWith((s) => { _cancellationTokenSource.Cancel(); });
        }


        /// <summary>
        /// Closes the listener after you exit the processing loop
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public Task CloseAsync()
        {
            _httpClient.Dispose();
            return _listener.CloseAsync(_cancellationTokenSource.Token);
        }


        /// <summary>
        /// Listener Response Handler
        /// </summary>
        /// <param name="context"></param>
        private async void ListenerRequestHandler(RelayedHttpListenerContext context)
        {
            var startTimeUtc = DateTime.UtcNow;
            try
            {
                Console.WriteLine("Calling {0}...", _targetServiceAddress);
                var requestMessage = this.CreateHttpRequestMessage(context);
                var responseMessage = await _httpClient.SendAsync(requestMessage);
                await this.SendResponseAsync(context, responseMessage);
            }

            catch (Exception ex)
            {
                Logger.LogException(ex);
                SendErrorResponse(ex, context);
            }
            finally
            {
                Logger.LogRequest(startTimeUtc);
                // Confirm the response has been sent
                context.Response.StatusCode = HttpStatusCode.OK;
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
        /// Sends the response to the server
        /// </summary>
        /// <param name="context"></param>
        /// <param name="responseMessage"></param>
        /// <returns></returns>
        private async Task SendResponseAsync(RelayedHttpListenerContext context, HttpResponseMessage responseMessage)
        {
            context.Response.StatusCode = responseMessage.StatusCode;
            context.Response.StatusDescription = responseMessage.ReasonPhrase;
            foreach (var header in responseMessage.Headers)
            {
                if (string.Equals(header.Key, "Transfer-Encoding"))
                {
                    continue;
                }

                context.Response.Headers.Add(header.Key, string.Join(",", header.Value));
            }

            var responseStream = await responseMessage.Content.ReadAsStreamAsync();
            await responseStream.CopyToAsync(context.Response.OutputStream);
        }


        /// <summary>
        /// Sends the error response
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="context"></param>
        private void SendErrorResponse(Exception ex, RelayedHttpListenerContext context)
        {
            context.Response.StatusCode = HttpStatusCode.InternalServerError;
            context.Response.StatusDescription = $"Internal Server Error: {ex.GetType().FullName}: {ex.Message}";
            context.Response.Close();
        }

        /// <summary>
        /// Creates the Http Request message
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private HttpRequestMessage CreateHttpRequestMessage(RelayedHttpListenerContext context)
        {
            var requestMessage = new HttpRequestMessage();
            if (context.Request.HasEntityBody)
            {
                requestMessage.Content = new StreamContent(context.Request.InputStream);
                // Experiment to see if I can capture the return message instead of having the bot responding directly (so far it doesn't work).
                //var contentStream = new MemoryStream();
                //var writer = new StreamWriter(contentStream);
                //var newActivity = requestMessage.Content.ReadAsStringAsync().Result.Replace("https://directline.botframework.com/", "https://localhost:44372/");
                //writer.Write(newActivity);
                //writer.Flush();
                //contentStream.Position = 0;
                //requestMessage.Content = new StreamContent(contentStream);
                var contentType = context.Request.Headers[HttpRequestHeader.ContentType];
                if (!string.IsNullOrEmpty(contentType))
                {
                    requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                }
            }

            var relativePath = context.Request.Url.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
            relativePath = relativePath.Replace(_hybridConnectionSubPath, string.Empty, StringComparison.OrdinalIgnoreCase);
            requestMessage.RequestUri = new Uri(relativePath, UriKind.RelativeOrAbsolute);
            requestMessage.Method = new HttpMethod(context.Request.HttpMethod);

            foreach (var headerName in context.Request.Headers.AllKeys)
            {
                if (string.Equals(headerName, "Host", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(headerName, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't flow these headers here
                    continue;
                }

                requestMessage.Headers.Add(headerName, context.Request.Headers[headerName]);
            }

            Logger.LogRequestActivity(requestMessage);

            return requestMessage;
        }
    }
}
