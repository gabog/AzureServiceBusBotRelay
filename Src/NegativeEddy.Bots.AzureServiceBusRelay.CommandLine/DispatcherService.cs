// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Relay;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NegativeEddy.Bots.AzureServiceBusRelay.CommandLine
{
    internal class DispatcherService : IHostedService
    {
        private HttpClient _httpClient;
        private string _hybridConnectionSubPath;
        private HybridConnectionListener _listener;
        private Uri _targetServiceAddress;
        private readonly CommandLineOptions _options;
        private readonly ILogger<DispatcherService> _logger;

        public DispatcherService(CommandLineOptions options, ILogger<DispatcherService> logger)
        {
            _options = options;
            _logger = logger;
        }

        private async void ListenerRequestHandler(RelayedHttpListenerContext context)
        {
            var startTimeUtc = DateTime.UtcNow;
            HttpStatusCode responseStatus = 0;
            try
            {
                
                _logger.LogInformation("Received message");
                var requestMessage = await CreateHttpRequestMessage(context);
                _logger.LogInformation($"{requestMessage.Method} to {_targetServiceAddress}");
                var responseMessage = await _httpClient.SendAsync(requestMessage);
                responseStatus = responseMessage.StatusCode;
                await SendResponseAsync(context, responseMessage);
                await context.Response.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                SendErrorResponse(ex, context);
            }
            finally
            {
                var stopTimeUtc = DateTime.UtcNow;
                double milliseconds = stopTimeUtc.Subtract(startTimeUtc).TotalMilliseconds;
                _logger.LogInformation("Response {0} took {1:N0} ms", responseStatus, milliseconds);
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

        private async Task LogRequestActivity(HttpRequestMessage requestMessage)
        {
            if (requestMessage.Content is null)
            {
                _logger.LogInformation("<no content>");
                return;
            }
            string content = await requestMessage.Content.ReadAsStringAsync();

            var formatted = content;

            try
            {
                // attempt to parse and pretty print as json
                var doc = JsonDocument.Parse(content);
                formatted = PrettyPrint(doc.RootElement, true);
            }
            catch { }

            _logger.LogDebug(formatted);
        }

        public static string PrettyPrint(JsonElement element, bool indent)
        => element.ValueKind == JsonValueKind.Undefined ? "" : JsonSerializer.Serialize(element, new JsonSerializerOptions { WriteIndented = indent });

        public async Task StartAsync(CancellationToken cancellationToken)
        {           
            _targetServiceAddress = new Uri(_options.TargetServiceAddress);

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(_options.PolicyName, _options.PolicyKey);
            _listener = new HybridConnectionListener(new Uri($"sb://{_options.RelayNamespace}/{_options.RelayName}"), tokenProvider);

            _httpClient = new HttpClient
            {
                BaseAddress = _targetServiceAddress
            };
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;

            _hybridConnectionSubPath = EnsureEndsWith(_listener.Address.AbsolutePath, "/");

            _listener.RequestHandler = ListenerRequestHandler;
            await _listener.OpenAsync(cancellationToken);

            _logger.LogInformation("Azure Service Bus is listening on {0}\nand routing requests to {1}", _listener.Address, _httpClient.BaseAddress);

            string EnsureEndsWith(string s, string endValue)
            {
                if (!string.IsNullOrEmpty(s) && s.EndsWith(endValue, StringComparison.Ordinal))
                {
                    return s;
                }

                return s + endValue;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
                _httpClient.Dispose();
                await _listener.CloseAsync(cancellationToken);
        }
    }
}