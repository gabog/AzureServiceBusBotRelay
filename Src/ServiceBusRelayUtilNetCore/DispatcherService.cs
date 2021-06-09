using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GaboG.ServiceBusRelayUtilNetCore.Extensions;
using Microsoft.Azure.Relay;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace GaboG.ServiceBusRelayUtilNetCore
{
    internal class DispatcherService : IHostedService
    {
        private HttpClient _httpClient;
        private string _hybridConnectionSubPath;
        private HybridConnectionListener _listener;
        private Uri _targetServiceAddress;
        private readonly IConfiguration _config;

        public DispatcherService(IConfiguration config)
        {
            _config = config;
        }

        private async void ListenerRequestHandler(RelayedHttpListenerContext context)
        {
            var startTimeUtc = DateTime.UtcNow;
            try
            {
                Console.WriteLine("Calling {0}...", _targetServiceAddress);
                var requestMessage = await CreateHttpRequestMessage(context);
                var responseMessage = await _httpClient.SendAsync(requestMessage);
                await SendResponseAsync(context, responseMessage);
                await context.Response.CloseAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
                SendErrorResponse(ex, context);
            }
            finally
            {
                LogRequest(startTimeUtc);
            }
        }

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

        private void SendErrorResponse(Exception ex, RelayedHttpListenerContext context)
        {
            context.Response.StatusCode = HttpStatusCode.InternalServerError;
            context.Response.StatusDescription = $"Internal Server Error: {ex.GetType().FullName}: {ex.Message}";
            context.Response.Close();
        }

        private async Task<HttpRequestMessage> CreateHttpRequestMessage(RelayedHttpListenerContext context)
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

            await LogRequestActivity(requestMessage);

            return requestMessage;
        }

        private void LogRequest(DateTime startTimeUtc)
        {
            var stopTimeUtc = DateTime.UtcNow;
            //var buffer = new StringBuilder();
            //buffer.Append($"{startTimeUtc.ToString("s", CultureInfo.InvariantCulture)}, ");
            //buffer.Append($"\"{context.Request.HttpMethod} {context.Request.Url.GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped)}\", ");
            //buffer.Append($"{(int)context.Response.StatusCode}, ");
            //buffer.Append($"{(int)stopTimeUtc.Subtract(startTimeUtc).TotalMilliseconds}");
            //Console.WriteLine(buffer);

            Console.WriteLine("...and back {0:N0} ms...", stopTimeUtc.Subtract(startTimeUtc).TotalMilliseconds);
            Console.WriteLine("");
        }

        private async Task LogRequestActivity(HttpRequestMessage requestMessage)
        {
            var content = await requestMessage.Content?.ReadAsStringAsync();
            if (content is null)
            {
                Console.WriteLine("<no content>");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;

            var formatted = content;

            try
            {
                // attempt to parse and pretty print as json
                var doc = JsonDocument.Parse(content);
                formatted = PrettyPrint(doc.RootElement, true);
            }
            catch { }

            Console.WriteLine(formatted);
            Console.ResetColor();
        }

        public static string PrettyPrint(JsonElement element, bool indent)
        => element.ValueKind == JsonValueKind.Undefined ? "" : JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = indent });


        private static void LogException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.WriteLine("");
            Console.ResetColor();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var config = _config;
            string relayNamespace = config["RelayNamespace"];
            string connectionName = config["RelayName"];
            string keyName = config["PolicyName"];
            string key = config["PolicyKey"];

            _targetServiceAddress = new Uri(config["TargetServiceAddress"]);

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);
            _listener = new HybridConnectionListener(new Uri($"sb://{relayNamespace}/{connectionName}"), tokenProvider);

            _httpClient = new HttpClient
            {
                BaseAddress = _targetServiceAddress
            };
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;

            _hybridConnectionSubPath = _listener.Address.AbsolutePath.EnsureEndsWith("/");

            _listener.RequestHandler = ListenerRequestHandler;
            await _listener.OpenAsync(cancellationToken);

            Console.WriteLine("Azure Service Bus is listening on \n\r\t{0}\n\rand routing requests to \n\r\t{1}\n\r\n\r", _listener.Address, _httpClient.BaseAddress);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
                _httpClient.Dispose();
                await _listener.CloseAsync(cancellationToken);
        }
    }
}